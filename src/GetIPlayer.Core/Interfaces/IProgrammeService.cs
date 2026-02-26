using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Service for searching and retrieving programme metadata from BBC APIs.
/// </summary>
public interface IProgrammeService
{
    /// <summary>
    /// Search for programmes matching a regex pattern.
    /// </summary>
    /// <param name="searchTerm">Regex pattern to match against programme names.</param>
    /// <param name="types">Programme types to search.</param>
    /// <param name="channelFilter">Optional regex filter for channel name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results.</returns>
    Task<SearchResult> SearchAsync(
        string searchTerm,
        ProgrammeType[] types,
        string? channelFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get full programme metadata by PID.
    /// </summary>
    /// <param name="pid">BBC programme identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Programme with full metadata, or null if not found.</returns>
    Task<Programme?> GetByPidAsync(string pid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available channels for a programme type.
    /// </summary>
    /// <param name="type">Programme type (TV or Radio).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available channels.</returns>
    Task<IReadOnlyList<ChannelInfo>> GetChannelsAsync(
        ProgrammeType type,
        CancellationToken cancellationToken = default);
}
