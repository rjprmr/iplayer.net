using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Streaming;

/// <summary>
/// Downloads media segments and concatenates them into a single file.
/// Handles retries for individual segment failures.
/// </summary>
public sealed partial class SegmentDownloader
{
    private readonly IHttpClientService _httpClient;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SegmentDownloader> _logger;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    public SegmentDownloader(
        IHttpClientService httpClient,
        IFileSystem fileSystem,
        ILogger<SegmentDownloader> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Download all segments and concatenate into the output file.
    /// Reports progress via the callback.
    /// </summary>
    public async Task<long> DownloadSegmentsAsync(
        IReadOnlyList<Uri> segmentUrls,
        string outputPath,
        IProgress<DownloadProgress>? progress = null,
        string pid = "",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var totalSegments = segmentUrls.Count;
        long totalBytes = 0;

        LogStartingDownload(_logger, totalSegments, outputPath);

        // Ensure output directory exists
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            _fileSystem.CreateDirectory(dir);
        }

        // Write segments sequentially to output file
        using var outputStream = _fileSystem.OpenWrite(outputPath);

        for (var i = 0; i < totalSegments; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var segmentData = await DownloadSegmentWithRetryAsync(
                segmentUrls[i], i, cancellationToken).ConfigureAwait(false);

            if (segmentData.Length > 0)
            {
                await outputStream.WriteAsync(segmentData, cancellationToken).ConfigureAwait(false);
                totalBytes += segmentData.Length;
            }

            // Report progress
            progress?.Report(new DownloadProgress
            {
                Pid = pid,
                TotalBytes = 0, // Unknown total ahead of time
                BytesDownloaded = totalBytes,
                Phase = $"Segment {i + 1}/{totalSegments}"
            });
        }

        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        LogDownloadComplete(_logger, totalSegments, totalBytes);
        return totalBytes;
    }

    private async Task<byte[]> DownloadSegmentWithRetryAsync(
        Uri segmentUrl,
        int segmentIndex,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await _httpClient.GetBytesAsync(
                    segmentUrl.AbsoluteUri, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                LogSegmentRetry(_logger, segmentIndex, attempt, MaxRetries, ex.Message);
                await Task.Delay(RetryDelayMs * attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                LogSegmentFailed(_logger, segmentIndex, ex.Message);
                throw;
            }
        }

        return []; // Unreachable, but satisfies compiler
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting download of {SegmentCount} segments to {OutputPath}")]
    private static partial void LogStartingDownload(ILogger logger, int segmentCount, string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Download complete: {SegmentCount} segments, {TotalBytes} bytes")]
    private static partial void LogDownloadComplete(ILogger logger, int segmentCount, long totalBytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Segment {SegmentIndex} retry {Attempt}/{MaxRetries}: {ErrorMessage}")]
    private static partial void LogSegmentRetry(ILogger logger, int segmentIndex, int attempt, int maxRetries, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Segment {SegmentIndex} failed after all retries: {ErrorMessage}")]
    private static partial void LogSegmentFailed(ILogger logger, int segmentIndex, string errorMessage);
}
