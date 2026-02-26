using GetIPlayer.Core.Interfaces;
using GetIPlayer.Infrastructure.Bbc;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.PostProcessing;

/// <summary>
/// Downloads programme thumbnails from the BBC image CDN.
/// </summary>
public sealed partial class ThumbnailDownloader
{
    private readonly IHttpClientService _httpClient;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ThumbnailDownloader> _logger;

    /// <summary>
    /// Available thumbnail sizes (width x height).
    /// </summary>
    private static readonly string[] ThumbnailSizes =
    [
        "1920x1080",
        "960x540",
        "640x360",
        "480x270",
        "320x180",
    ];

    public ThumbnailDownloader(
        IHttpClientService httpClient,
        IFileSystem fileSystem,
        ILogger<ThumbnailDownloader> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Download the best available thumbnail for a programme.
    /// </summary>
    /// <param name="thumbnailUrl">BBC thumbnail URL template (with {recipe} placeholder, or direct URL).</param>
    /// <param name="outputPath">Path to save the thumbnail image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if thumbnail was downloaded successfully.</returns>
    public async Task<bool> DownloadAsync(
        string? thumbnailUrl,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(thumbnailUrl))
        {
            LogNoThumbnail(_logger);
            return false;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        // Ensure output directory exists
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            _fileSystem.CreateDirectory(dir);
        }

        // Try sizes from largest to smallest
        foreach (var size in ThumbnailSizes)
        {
            var url = thumbnailUrl.Contains("{recipe}", StringComparison.OrdinalIgnoreCase)
                ? thumbnailUrl.Replace("{recipe}", size, StringComparison.OrdinalIgnoreCase)
                : thumbnailUrl;

            try
            {
                LogDownloadingThumbnail(_logger, url, size);
                await _httpClient.DownloadToFileAsync(url, outputPath, null, cancellationToken).ConfigureAwait(false);

                if (_fileSystem.FileExists(outputPath) && _fileSystem.GetFileSize(outputPath) > 0)
                {
                    LogThumbnailSaved(_logger, outputPath, size);
                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                LogThumbnailFailed(_logger, size, ex.Message);
            }

            // If the URL doesn't contain {recipe}, no point trying other sizes
            if (!thumbnailUrl.Contains("{recipe}", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return false;
    }

    /// <summary>
    /// Download a thumbnail by PID using the standard BBC image URL pattern.
    /// </summary>
    public async Task<bool> DownloadByPidAsync(
        string imagePid,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        PidValidator.EnsureValid(imagePid);
        var url = $"https://ichef.bbci.co.uk/images/ic/{{recipe}}/{Uri.EscapeDataString(imagePid)}.jpg";
        return await DownloadAsync(url, outputPath, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No thumbnail URL available")]
    private static partial void LogNoThumbnail(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Downloading thumbnail from {Url} (size: {Size})")]
    private static partial void LogDownloadingThumbnail(ILogger logger, string url, string size);

    [LoggerMessage(Level = LogLevel.Information, Message = "Thumbnail saved: {OutputPath} (size: {Size})")]
    private static partial void LogThumbnailSaved(ILogger logger, string outputPath, string size);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Thumbnail size {Size} not available: {ErrorMessage}")]
    private static partial void LogThumbnailFailed(ILogger logger, string size, string errorMessage);
}
