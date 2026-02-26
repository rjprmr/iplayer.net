using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Manages the programme index cache (local copy of BBC schedule data).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get all cached programmes of a given type.
    /// </summary>
    /// <param name="type">Programme type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All cached programmes.</returns>
    Task<IReadOnlyList<Programme>> GetAllAsync(
        ProgrammeType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh the cache from BBC APIs.
    /// </summary>
    /// <param name="type">Programme type to refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshAsync(ProgrammeType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the cache is stale and needs refreshing.
    /// </summary>
    /// <param name="type">Programme type.</param>
    /// <returns>True if cache needs refresh.</returns>
    Task<bool> IsStaleAsync(ProgrammeType type);

    /// <summary>
    /// Get a programme by its cache index number.
    /// </summary>
    /// <param name="index">Cache index.</param>
    /// <param name="type">Programme type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Programme at the given index, or null.</returns>
    Task<Programme?> GetByIndexAsync(int index, ProgrammeType type, CancellationToken cancellationToken = default);
}
