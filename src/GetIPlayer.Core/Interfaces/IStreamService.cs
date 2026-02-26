using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Discovers and resolves available streams for a programme.
/// </summary>
public interface IStreamService
{
    /// <summary>
    /// Get available streams for a programme, ordered by quality preference.
    /// </summary>
    /// <param name="pid">BBC programme identifier.</param>
    /// <param name="qualityPreferences">Quality levels in priority order.</param>
    /// <param name="versions">Version names to try (e.g. "original", "signed").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered list of available streams.</returns>
    Task<IReadOnlyList<StreamInfo>> GetStreamsAsync(
        string pid,
        QualityLevel[] qualityPreferences,
        string[] versions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get version PIDs for a programme.
    /// </summary>
    /// <param name="pid">BBC programme identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available versions.</returns>
    Task<IReadOnlyList<StreamVersion>> GetVersionsAsync(
        string pid,
        CancellationToken cancellationToken = default);
}
