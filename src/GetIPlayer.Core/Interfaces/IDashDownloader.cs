using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Downloads MPEG-DASH content.
/// </summary>
public interface IDashDownloader
{
    /// <summary>
    /// Download a DASH stream to a file.
    /// </summary>
    /// <param name="stream">Stream info with the MPD manifest URL.</param>
    /// <param name="outputPath">Output file path for the raw segments.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total bytes downloaded.</returns>
    Task<long> DownloadAsync(
        StreamInfo stream,
        string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
