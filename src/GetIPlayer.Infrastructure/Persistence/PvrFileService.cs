using System.Text.Json;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Persistence;

/// <summary>
/// JSON file-based PVR saved search persistence.
/// Each PVR search is stored as its own JSON file in the PVR directory.
/// </summary>
public sealed partial class PvrFileService : IPvrService
{
    private readonly IFileSystem _fileSystem;
    private readonly IDownloadService _downloadService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PvrFileService> _logger;
    private readonly string _pvrDir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public PvrFileService(
        IFileSystem fileSystem,
        IDownloadService downloadService,
        ICacheService cacheService,
        ILogger<PvrFileService> logger,
        string pvrDir)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pvrDir = pvrDir ?? throw new ArgumentNullException(nameof(pvrDir));

        _fileSystem.CreateDirectory(_pvrDir);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DownloadResult>> RunAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var searches = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        var enabledSearches = searches.Where(s => s.IsEnabled).ToList();
        var results = new List<DownloadResult>();

        LogRunningPvr(_logger, enabledSearches.Count);

        foreach (var search in enabledSearches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LogRunningSearch(_logger, search.Name, search.SearchTerm);

            try
            {
                // Get cached programmes and search
                var programmes = await _cacheService.GetAllAsync(search.Type ?? ProgrammeType.Tv, cancellationToken).ConfigureAwait(false);

                var matches = programmes
                    .Where(p => p.Name.Contains(search.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                p.Episode.Contains(search.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                p.Description.Contains(search.SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .Where(p => string.IsNullOrEmpty(search.ChannelFilter) ||
                                p.Channel.Contains(search.ChannelFilter, StringComparison.OrdinalIgnoreCase));

                foreach (var programme in matches)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await _downloadService.DownloadAsync(
                        programme.Pid, progress, cancellationToken).ConfigureAwait(false);
                    results.Add(result);
                }

                // Update last run time
                var updated = search with { LastRunAt = DateTimeOffset.UtcNow };
                await SaveSearchAsync(updated, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogSearchError(_logger, search.Name, ex.Message);
            }
        }

        LogPvrComplete(_logger, results.Count);
        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task AddAsync(PvrSearch search, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentException.ThrowIfNullOrWhiteSpace(search.Name);

        var updated = search with { CreatedAt = DateTimeOffset.UtcNow };
        await SaveSearchAsync(updated, cancellationToken).ConfigureAwait(false);
        LogSearchAdded(_logger, search.Name);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var path = GetSearchPath(name);
        if (_fileSystem.FileExists(path))
        {
            _fileSystem.DeleteFile(path);
            LogSearchDeleted(_logger, name);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PvrSearch>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.DirectoryExists(_pvrDir))
        {
            return [];
        }

        var searches = new List<PvrSearch>();
        var files = _fileSystem.GetFiles(_pvrDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await _fileSystem.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                var search = JsonSerializer.Deserialize<PvrSearch>(json, JsonOptions);
                if (search is not null)
                {
                    searches.Add(search);
                }
            }
            catch (JsonException ex)
            {
                LogFileCorrupt(_logger, file, ex.Message);
            }
        }

        return searches.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<PvrSearch?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var path = GetSearchPath(name);
        if (!_fileSystem.FileExists(path))
        {
            return null;
        }

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<PvrSearch>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetEnabledAsync(string name, bool enabled, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var search = await GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
        if (search is not null)
        {
            var updated = search with { IsEnabled = enabled };
            await SaveSearchAsync(updated, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SaveSearchAsync(PvrSearch search, CancellationToken cancellationToken)
    {
        var path = GetSearchPath(search.Name);
        var json = JsonSerializer.Serialize(search, JsonOptions);
        await _fileSystem.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    private string GetSearchPath(string name)
    {
        var safeName = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        if (string.IsNullOrEmpty(safeName))
        {
            safeName = "search";
        }

        return _fileSystem.SafeCombinePath(_pvrDir, $"{safeName}.json");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Running {Count} PVR searches")]
    private static partial void LogRunningPvr(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Running PVR search '{Name}': {SearchTerm}")]
    private static partial void LogRunningSearch(ILogger logger, string name, string searchTerm);

    [LoggerMessage(Level = LogLevel.Warning, Message = "PVR search '{Name}' failed: {ErrorMessage}")]
    private static partial void LogSearchError(ILogger logger, string name, string errorMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "PVR complete: {Count} downloads")]
    private static partial void LogPvrComplete(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "PVR search added: {Name}")]
    private static partial void LogSearchAdded(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "PVR search deleted: {Name}")]
    private static partial void LogSearchDeleted(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "PVR file corrupt {FilePath}: {ErrorMessage}")]
    private static partial void LogFileCorrupt(ILogger logger, string filePath, string errorMessage);
}
