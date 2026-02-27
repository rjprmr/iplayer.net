using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Application;

/// <summary>
/// Orchestrates PVR (automated recording) operations.
/// </summary>
public sealed partial class PvrOrchestrator
{
    private readonly IPvrService _pvrService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PvrOrchestrator> _logger;

    public PvrOrchestrator(
        IPvrService pvrService,
        ICacheService cacheService,
        ILogger<PvrOrchestrator> logger)
    {
        _pvrService = pvrService ?? throw new ArgumentNullException(nameof(pvrService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Run all enabled PVR searches and download matches.
    /// Refreshes cache before running.
    /// </summary>
    public async Task<IReadOnlyList<DownloadResult>> RunAllAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        LogPvrRunStarting(_logger);

        // Refresh cache first to ensure we have latest listings
        await _cacheService.RefreshAsync(Core.Enums.ProgrammeType.Tv, cancellationToken).ConfigureAwait(false);
        await _cacheService.RefreshAsync(Core.Enums.ProgrammeType.Radio, cancellationToken).ConfigureAwait(false);

        var results = await _pvrService.RunAsync(progress, cancellationToken).ConfigureAwait(false);

        var successful = results.Count(r => r.Status == Core.Enums.DownloadStatus.Success);
        LogPvrRunComplete(_logger, results.Count, successful);

        return results;
    }

    /// <summary>
    /// Add a new PVR saved search.
    /// </summary>
    public async Task AddSearchAsync(PvrSearch search, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search);
        await _pvrService.AddAsync(search, cancellationToken).ConfigureAwait(false);
        LogPvrSearchAdded(_logger, search.Name, search.SearchTerm);
    }

    /// <summary>
    /// Delete a PVR saved search.
    /// </summary>
    public async Task DeleteSearchAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await _pvrService.DeleteAsync(name, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// List all PVR saved searches.
    /// </summary>
    public async Task<IReadOnlyList<PvrSearch>> ListSearchesAsync(CancellationToken cancellationToken = default)
    {
        return await _pvrService.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Enable or disable a PVR search.
    /// </summary>
    public async Task SetSearchEnabledAsync(string name, bool enabled, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await _pvrService.SetEnabledAsync(name, enabled, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting PVR run")]
    private static partial void LogPvrRunStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "PVR run complete: {TotalResults} results, {SuccessCount} successful")]
    private static partial void LogPvrRunComplete(ILogger logger, int totalResults, int successCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "PVR search added: '{Name}' term='{SearchTerm}'")]
    private static partial void LogPvrSearchAdded(ILogger logger, string name, string searchTerm);
}
