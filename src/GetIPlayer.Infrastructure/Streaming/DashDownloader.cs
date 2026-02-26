using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Streaming;

/// <summary>
/// Downloads MPEG-DASH streams by parsing MPD manifests and downloading segments.
/// </summary>
public sealed partial class DashDownloader : IDashDownloader
{
    private readonly IHttpClientService _httpClient;
    private readonly SegmentDownloader _segmentDownloader;
    private readonly ILogger<DashDownloader> _logger;

    public DashDownloader(
        IHttpClientService httpClient,
        SegmentDownloader segmentDownloader,
        ILogger<DashDownloader> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _segmentDownloader = segmentDownloader ?? throw new ArgumentNullException(nameof(segmentDownloader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<long> DownloadAsync(
        StreamInfo stream,
        string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        LogStartingDashDownload(_logger, stream.StreamUrl.AbsoluteUri, stream.Mode);

        // Fetch MPD manifest
        var mpdContent = await _httpClient.GetStringAsync(
            stream.StreamUrl.AbsoluteUri, cancellationToken).ConfigureAwait(false);

        var adaptationSets = MpdParser.ParseMpd(mpdContent, stream.StreamUrl);
        if (adaptationSets.Count == 0)
        {
            throw new InvalidOperationException("No adaptation sets found in MPD manifest");
        }

        long totalBytes = 0;

        // Download video adaptation set (highest bandwidth)
        var videoSet = adaptationSets
            .FirstOrDefault(a => a.MimeType.Contains("video", StringComparison.OrdinalIgnoreCase));

        if (videoSet is not null)
        {
            var bestVideo = videoSet.Representations
                .OrderByDescending(r => r.Bandwidth)
                .First();

            LogSelectedRepresentation(_logger, "video", bestVideo.Bandwidth, bestVideo.Width ?? 0, bestVideo.Height ?? 0);

            if (bestVideo.SegmentUrls.Count > 0)
            {
                var videoPath = outputPath + ".video.tmp";
                totalBytes += await _segmentDownloader.DownloadSegmentsAsync(
                    bestVideo.SegmentUrls, videoPath, progress, stream.Mode, cancellationToken).ConfigureAwait(false);
            }
            else if (bestVideo.BaseUrl is not null)
            {
                // Single file download
                var videoPath = outputPath + ".video.tmp";
                await _httpClient.DownloadToFileAsync(
                    bestVideo.BaseUrl.AbsoluteUri, videoPath, null, cancellationToken).ConfigureAwait(false);
            }
        }

        // Download audio adaptation set (highest bandwidth)
        var audioSet = adaptationSets
            .FirstOrDefault(a => a.MimeType.Contains("audio", StringComparison.OrdinalIgnoreCase));

        if (audioSet is not null)
        {
            var bestAudio = audioSet.Representations
                .OrderByDescending(r => r.Bandwidth)
                .First();

            LogSelectedRepresentation(_logger, "audio", bestAudio.Bandwidth, 0, 0);

            if (bestAudio.SegmentUrls.Count > 0)
            {
                var audioPath = outputPath + ".audio.tmp";
                totalBytes += await _segmentDownloader.DownloadSegmentsAsync(
                    bestAudio.SegmentUrls, audioPath, null, stream.Mode, cancellationToken).ConfigureAwait(false);
            }
            else if (bestAudio.BaseUrl is not null)
            {
                var audioPath = outputPath + ".audio.tmp";
                await _httpClient.DownloadToFileAsync(
                    bestAudio.BaseUrl.AbsoluteUri, audioPath, null, cancellationToken).ConfigureAwait(false);
            }
        }

        LogDashDownloadComplete(_logger, totalBytes);
        return totalBytes;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting DASH download from {Url} mode={Mode}")]
    private static partial void LogStartingDashDownload(ILogger logger, string url, string mode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Selected {TrackType} representation: bandwidth={Bandwidth}, resolution={Width}x{Height}")]
    private static partial void LogSelectedRepresentation(ILogger logger, string trackType, int bandwidth, int width, int height);

    [LoggerMessage(Level = LogLevel.Information, Message = "DASH download complete: {TotalBytes} bytes")]
    private static partial void LogDashDownloadComplete(ILogger logger, long totalBytes);
}
