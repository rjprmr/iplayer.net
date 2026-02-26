using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Application;

/// <summary>
/// Orchestrates programme search operations across cached and live data.
/// </summary>
public sealed partial class SearchOrchestrator
{
    private readonly IProgrammeService _programmeService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SearchOrchestrator> _logger;

    public SearchOrchestrator(
        IProgrammeService programmeService,
        ICacheService cacheService,
        ILogger<SearchOrchestrator> logger)
    {
        _programmeService = programmeService ?? throw new ArgumentNullException(nameof(programmeService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search for programmes, refreshing cache if stale.
    /// </summary>
    public async Task<SearchResult> SearchAsync(
        string searchTerm,
        ProgrammeType[] types,
        string? channelFilter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        if (types.Length == 0)
        {
            types = [ProgrammeType.Tv, ProgrammeType.Radio];
        }

        LogSearching(_logger, searchTerm, types.Length);

        // Ensure cache is fresh
        foreach (var type in types)
        {
            if (await _cacheService.IsStaleAsync(type).ConfigureAwait(false))
            {
                LogRefreshingCache(_logger, type);
                await _cacheService.RefreshAsync(type, cancellationToken).ConfigureAwait(false);
            }
        }

        // Search across all requested types
        var allMatches = new List<Programme>();
        foreach (var type in types)
        {
            var cached = await _cacheService.GetAllAsync(type, cancellationToken).ConfigureAwait(false);

            var matches = cached
                .Where(p => MatchesSearchTerm(p, searchTerm))
                .Where(p => string.IsNullOrEmpty(channelFilter) ||
                            p.Channel.Contains(channelFilter, StringComparison.OrdinalIgnoreCase));

            allMatches.AddRange(matches);
        }

        LogSearchResults(_logger, searchTerm, allMatches.Count);

        return new SearchResult
        {
            Programmes = allMatches.AsReadOnly(),
            TotalCount = allMatches.Count,
            Page = 1,
            PageSize = allMatches.Count
        };
    }

    /// <summary>
    /// Get detailed info about a specific programme.
    /// </summary>
    public async Task<Programme?> GetInfoAsync(string pid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pid);
        return await _programmeService.GetByPidAsync(pid, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Refresh the programme cache for one or more types.
    /// </summary>
    public async Task RefreshCacheAsync(
        ProgrammeType[] types,
        CancellationToken cancellationToken = default)
    {
        if (types.Length == 0)
        {
            types = [ProgrammeType.Tv, ProgrammeType.Radio];
        }

        foreach (var type in types)
        {
            LogRefreshingCache(_logger, type);
            await _cacheService.RefreshAsync(type, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool MatchesSearchTerm(Programme programme, string searchTerm)
    {
        // Simple contains search (could be extended to regex)
        return programme.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               programme.Episode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               programme.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(programme.Pid, searchTerm, StringComparison.OrdinalIgnoreCase);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Searching for '{SearchTerm}' across {TypeCount} types")]
    private static partial void LogSearching(ILogger logger, string searchTerm, int typeCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshing cache for {Type}")]
    private static partial void LogRefreshingCache(ILogger logger, ProgrammeType type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Search for '{SearchTerm}' found {Count} results")]
    private static partial void LogSearchResults(ILogger logger, string searchTerm, int count);
}
