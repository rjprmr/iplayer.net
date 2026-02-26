using System.Text.Json;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Persistence;

/// <summary>
/// JSON file-based programme index cache.
/// Stores programme listings locally to avoid re-fetching from BBC on every operation.
/// </summary>
public sealed partial class JsonCacheService : ICacheService
{
    private readonly IFileSystem _fileSystem;
    private readonly IProgrammeService _programmeService;
    private readonly ILogger<JsonCacheService> _logger;
    private readonly string _cacheDir;
    private readonly TimeSpan _cacheMaxAge;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JsonCacheService(
        IFileSystem fileSystem,
        IProgrammeService programmeService,
        ILogger<JsonCacheService> logger,
        string cacheDir,
        TimeSpan? cacheMaxAge = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _programmeService = programmeService ?? throw new ArgumentNullException(nameof(programmeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheDir = cacheDir ?? throw new ArgumentNullException(nameof(cacheDir));
        _cacheMaxAge = cacheMaxAge ?? TimeSpan.FromHours(4);

        _fileSystem.CreateDirectory(_cacheDir);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Programme>> GetAllAsync(
        ProgrammeType type,
        CancellationToken cancellationToken = default)
    {
        var cachePath = GetCachePath(type);

        if (!_fileSystem.FileExists(cachePath))
        {
            LogCacheMiss(_logger, type);
            return [];
        }

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(cachePath, cancellationToken).ConfigureAwait(false);
            var programmes = JsonSerializer.Deserialize<List<Programme>>(json, JsonOptions);
            LogCacheHit(_logger, type, programmes?.Count ?? 0);
            return programmes?.AsReadOnly() ?? (IReadOnlyList<Programme>)[];
        }
        catch (JsonException ex)
        {
            LogCacheCorrupt(_logger, cachePath, ex.Message);
            _fileSystem.DeleteFile(cachePath);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task RefreshAsync(ProgrammeType type, CancellationToken cancellationToken = default)
    {
        LogRefreshing(_logger, type);

        var allProgrammes = new List<Programme>();
        var page = 1;
        const int pageSize = 50;
        var hasMore = true;

        while (hasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use a broad search to populate the cache
            var result = await _programmeService.SearchAsync(
                "*", [type], null, cancellationToken).ConfigureAwait(false);

            allProgrammes.AddRange(result.Programmes);
            hasMore = result.Programmes.Count == pageSize && page * pageSize < result.TotalCount;
            page++;

            // Safety limit
            if (page > 200) break;
        }

        // Index programmes
        for (var i = 0; i < allProgrammes.Count; i++)
        {
            allProgrammes[i] = allProgrammes[i] with { Index = i + 1 };
        }

        var cachePath = GetCachePath(type);
        var json = JsonSerializer.Serialize(allProgrammes, JsonOptions);
        await _fileSystem.WriteAllTextAsync(cachePath, json, cancellationToken).ConfigureAwait(false);

        LogRefreshed(_logger, type, allProgrammes.Count);
    }

    /// <inheritdoc />
    public Task<bool> IsStaleAsync(ProgrammeType type)
    {
        var cachePath = GetCachePath(type);

        if (!_fileSystem.FileExists(cachePath))
        {
            return Task.FromResult(true);
        }

        try
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(cachePath);
            var isStale = DateTime.UtcNow - lastWriteTime > _cacheMaxAge;
            return Task.FromResult(isStale);
        }
        catch (IOException)
        {
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public async Task<Programme?> GetByIndexAsync(
        int index,
        ProgrammeType type,
        CancellationToken cancellationToken = default)
    {
        if (index < 1)
        {
            return null;
        }

        var programmes = await GetAllAsync(type, cancellationToken).ConfigureAwait(false);
        return programmes.FirstOrDefault(p => p.Index == index);
    }

    private string GetCachePath(ProgrammeType type)
    {
        var typeName = type switch
        {
            ProgrammeType.Tv => "tv",
            ProgrammeType.Radio => "radio",
            _ => "unknown"
        };
        return _fileSystem.SafeCombinePath(_cacheDir, $"{typeName}.json");
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache miss for type {Type}")]
    private static partial void LogCacheMiss(ILogger logger, ProgrammeType type);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for type {Type}: {Count} programmes")]
    private static partial void LogCacheHit(ILogger logger, ProgrammeType type, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cache file corrupt, deleting {CachePath}: {ErrorMessage}")]
    private static partial void LogCacheCorrupt(ILogger logger, string cachePath, string errorMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Refreshing cache for type {Type}")]
    private static partial void LogRefreshing(ILogger logger, ProgrammeType type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache refreshed for type {Type}: {Count} programmes")]
    private static partial void LogRefreshed(ILogger logger, ProgrammeType type, int count);
}
