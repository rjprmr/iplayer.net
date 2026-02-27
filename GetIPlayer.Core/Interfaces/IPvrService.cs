using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Manages PVR (Personal Video Recorder) saved searches.
/// </summary>
public interface IPvrService
{
    /// <summary>
    /// Run all enabled PVR searches and download matching programmes.
    /// </summary>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Download results for all matched programmes.</returns>
    Task<IReadOnlyList<DownloadResult>> RunAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new PVR saved search.
    /// </summary>
    Task AddAsync(PvrSearch search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a PVR saved search by name.
    /// </summary>
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all PVR saved searches.
    /// </summary>
    Task<IReadOnlyList<PvrSearch>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a PVR saved search by name.
    /// </summary>
    Task<PvrSearch?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable or disable a PVR search.
    /// </summary>
    Task SetEnabledAsync(string name, bool enabled, CancellationToken cancellationToken = default);
}
