using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Manages the download history.
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// Add a record to the download history.
    /// </summary>
    Task AddAsync(HistoryRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a programme PID has already been downloaded.
    /// </summary>
    /// <param name="pid">Programme PID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the PID is in the download history.</returns>
    Task<bool> ExistsAsync(string pid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all history records.
    /// </summary>
    Task<IReadOnlyList<HistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific history record by PID.
    /// </summary>
    Task<HistoryRecord?> GetByPidAsync(string pid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the download history.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
