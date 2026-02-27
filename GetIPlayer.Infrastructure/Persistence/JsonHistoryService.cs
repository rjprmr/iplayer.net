using System.Text.Json;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Persistence;

/// <summary>
/// JSON file-based download history service.
/// Stores download records in a single JSON file.
/// </summary>
public sealed partial class JsonHistoryService : IHistoryService, IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<JsonHistoryService> _logger;
    private readonly string _historyPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JsonHistoryService(
        IFileSystem fileSystem,
        ILogger<JsonHistoryService> logger,
        string historyPath)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _historyPath = historyPath ?? throw new ArgumentNullException(nameof(historyPath));

        // Ensure directory exists
        var dir = Path.GetDirectoryName(_historyPath);
        if (!string.IsNullOrEmpty(dir))
        {
            _fileSystem.CreateDirectory(dir);
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(HistoryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = await LoadRecordsAsync(cancellationToken).ConfigureAwait(false);

            // Replace existing record for same PID or add new
            var existing = records.FindIndex(r => string.Equals(r.Pid, record.Pid, StringComparison.Ordinal));
            if (existing >= 0)
            {
                records[existing] = record;
                LogHistoryUpdated(_logger, record.Pid);
            }
            else
            {
                records.Add(record);
                LogHistoryAdded(_logger, record.Pid);
            }

            await SaveRecordsAsync(records, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string pid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pid);

        var records = await LoadRecordsAsync(cancellationToken).ConfigureAwait(false);
        return records.Any(r => string.Equals(r.Pid, pid, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await LoadRecordsAsync(cancellationToken).ConfigureAwait(false);
        return records.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<HistoryRecord?> GetByPidAsync(string pid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pid);

        var records = await LoadRecordsAsync(cancellationToken).ConfigureAwait(false);
        return records.FirstOrDefault(r => string.Equals(r.Pid, pid, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_fileSystem.FileExists(_historyPath))
            {
                _fileSystem.DeleteFile(_historyPath);
                LogHistoryCleared(_logger);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<HistoryRecord>> LoadRecordsAsync(CancellationToken cancellationToken)
    {
        if (!_fileSystem.FileExists(_historyPath))
        {
            return [];
        }

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(_historyPath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<HistoryRecord>>(json, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            LogHistoryCorrupt(_logger, ex.Message);
            return [];
        }
    }

    private async Task SaveRecordsAsync(List<HistoryRecord> records, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(records, JsonOptions);
        await _fileSystem.WriteAllTextAsync(_historyPath, json, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "History record added for {Pid}")]
    private static partial void LogHistoryAdded(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Debug, Message = "History record updated for {Pid}")]
    private static partial void LogHistoryUpdated(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Information, Message = "Download history cleared")]
    private static partial void LogHistoryCleared(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "History file corrupt, starting fresh: {ErrorMessage}")]
    private static partial void LogHistoryCorrupt(ILogger logger, string errorMessage);

    /// <inheritdoc />
    public void Dispose()
    {
        _lock.Dispose();
    }
}
