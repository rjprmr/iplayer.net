using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Orchestrates the complete download workflow for a programme.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Download a programme by PID.
    /// </summary>
    /// <param name="pid">BBC programme identifier.</param>
    /// <param name="progress">Progress reporter for UI feedback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Download result.</returns>
    Task<DownloadResult> DownloadAsync(
        string pid,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a programme by index number from cache.
    /// </summary>
    /// <param name="index">Cache index number.</param>
    /// <param name="progress">Progress reporter for UI feedback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Download result.</returns>
    Task<DownloadResult> DownloadByIndexAsync(
        int index,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a programme from a BBC iPlayer/Sounds URL.
    /// </summary>
    /// <param name="url">BBC programme URL.</param>
    /// <param name="progress">Progress reporter for UI feedback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Download result.</returns>
    Task<DownloadResult> DownloadByUrlAsync(
        string url,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
