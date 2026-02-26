using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Downloads HLS (HTTP Live Streaming) content.
/// </summary>
public interface IHlsDownloader
{
    /// <summary>
    /// Download an HLS stream to a file.
    /// </summary>
    /// <param name="stream">Stream info with the M3U8 playlist URL.</param>
    /// <param name="outputPath">Output file path for the raw MPEG-TS data.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total bytes downloaded.</returns>
    Task<long> DownloadAsync(
        StreamInfo stream,
        string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
