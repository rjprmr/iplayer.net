using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Streaming;

/// <summary>
/// Downloads HLS streams by parsing M3U8 playlists and downloading segments.
/// </summary>
public sealed partial class HlsDownloader : IHlsDownloader
{
    private readonly IHttpClientService _httpClient;
    private readonly SegmentDownloader _segmentDownloader;
    private readonly ILogger<HlsDownloader> _logger;

    public HlsDownloader(
        IHttpClientService httpClient,
        SegmentDownloader segmentDownloader,
        ILogger<HlsDownloader> logger)
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

        LogStartingHlsDownload(_logger, stream.StreamUrl.AbsoluteUri, stream.Mode);

        // Fetch master playlist
        var masterContent = await _httpClient.GetStringAsync(
            stream.StreamUrl.AbsoluteUri, cancellationToken).ConfigureAwait(false);

        Uri mediaPlaylistUri;

        if (M3u8Parser.IsMasterPlaylist(masterContent))
        {
            // Select best variant based on bandwidth
            var variants = M3u8Parser.ParseMasterPlaylist(masterContent, stream.StreamUrl);
            if (variants.Count == 0)
            {
                throw new InvalidOperationException("No variant streams found in master playlist");
            }

            var bestVariant = variants.OrderByDescending(v => v.Bandwidth).First();
            LogSelectedVariant(_logger, bestVariant.Bandwidth, bestVariant.Width ?? 0, bestVariant.Height ?? 0);
            mediaPlaylistUri = bestVariant.Url;
        }
        else
        {
            // Content is already a media playlist
            mediaPlaylistUri = stream.StreamUrl;
        }

        // Fetch media playlist
        var mediaContent = await _httpClient.GetStringAsync(
            mediaPlaylistUri.AbsoluteUri, cancellationToken).ConfigureAwait(false);

        var segments = M3u8Parser.ParseMediaPlaylist(mediaContent, mediaPlaylistUri);
        LogSegmentCount(_logger, segments.Count);

        if (segments.Count == 0)
        {
            throw new InvalidOperationException("No segments found in media playlist");
        }

        var segmentUrls = segments.Select(s => s.Url).ToList();

        return await _segmentDownloader.DownloadSegmentsAsync(
            segmentUrls, outputPath, progress, stream.Mode, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting HLS download from {Url} mode={Mode}")]
    private static partial void LogStartingHlsDownload(ILogger logger, string url, string mode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Selected variant: bandwidth={Bandwidth}, resolution={Width}x{Height}")]
    private static partial void LogSelectedVariant(ILogger logger, int bandwidth, int width, int height);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Media playlist contains {Count} segments")]
    private static partial void LogSegmentCount(ILogger logger, int count);
}
