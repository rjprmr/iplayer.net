using GetIPlayer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Persistence;

/// <summary>
/// File-based lock service to prevent concurrent downloads of the same programme.
/// Uses advisory lock files with PID tracking.
/// </summary>
public sealed partial class LockFileService : IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LockFileService> _logger;
    private readonly string _lockDir;
    private readonly List<string> _activeLocks = [];
    private readonly object _lockObj = new();

    public LockFileService(
        IFileSystem fileSystem,
        ILogger<LockFileService> logger,
        string lockDir)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lockDir = lockDir ?? throw new ArgumentNullException(nameof(lockDir));

        _fileSystem.CreateDirectory(_lockDir);
    }

    /// <summary>
    /// Attempt to acquire a lock for a programme PID.
    /// </summary>
    /// <param name="pid">Programme PID.</param>
    /// <returns>True if the lock was acquired; false if another process holds it.</returns>
    public bool TryAcquire(string pid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pid);

        var lockPath = GetLockPath(pid);

        lock (_lockObj)
        {
            if (_fileSystem.FileExists(lockPath))
            {
                // Check if lock is stale (> 1 hour)
                try
                {
                    var lastWrite = File.GetLastWriteTimeUtc(lockPath);
                    if (DateTime.UtcNow - lastWrite > TimeSpan.FromHours(1))
                    {
                        LogStaleLock(_logger, pid, lockPath);
                        _fileSystem.DeleteFile(lockPath);
                    }
                    else
                    {
                        LogLockExists(_logger, pid);
                        return false;
                    }
                }
                catch (IOException)
                {
                    return false;
                }
            }

            try
            {
                // Create lock file with our PID
                using var stream = _fileSystem.OpenWrite(lockPath);
                using var writer = new StreamWriter(stream);
                writer.Write(Environment.ProcessId);
                writer.Flush();

                _activeLocks.Add(lockPath);
                LogLockAcquired(_logger, pid);
                return true;
            }
            catch (IOException ex)
            {
                LogLockFailed(_logger, pid, ex.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// Release a lock for a programme PID.
    /// </summary>
    public void Release(string pid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pid);

        var lockPath = GetLockPath(pid);

        lock (_lockObj)
        {
            if (_fileSystem.FileExists(lockPath))
            {
                _fileSystem.DeleteFile(lockPath);
            }

            _activeLocks.Remove(lockPath);
            LogLockReleased(_logger, pid);
        }
    }

    /// <summary>
    /// Check if a lock is held for a PID.
    /// </summary>
    public bool IsLocked(string pid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pid);
        var lockPath = GetLockPath(pid);
        return _fileSystem.FileExists(lockPath);
    }

    /// <summary>
    /// Release all locks held by this instance.
    /// </summary>
    public void Dispose()
    {
        lock (_lockObj)
        {
            foreach (var lockPath in _activeLocks)
            {
                try
                {
                    if (_fileSystem.FileExists(lockPath))
                    {
                        _fileSystem.DeleteFile(lockPath);
                    }
                }
                catch (IOException)
                {
                    // Best effort cleanup
                }
            }

            _activeLocks.Clear();
        }
    }

    private string GetLockPath(string pid)
    {
        var safePid = string.Concat(pid.Where(c => char.IsLetterOrDigit(c)));
        return _fileSystem.SafeCombinePath(_lockDir, $"{safePid}.lock");
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Lock acquired for {Pid}")]
    private static partial void LogLockAcquired(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Lock released for {Pid}")]
    private static partial void LogLockReleased(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Lock exists for {Pid}, skipping")]
    private static partial void LogLockExists(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stale lock detected for {Pid} at {LockPath}, removing")]
    private static partial void LogStaleLock(ILogger logger, string pid, string lockPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to acquire lock for {Pid}: {ErrorMessage}")]
    private static partial void LogLockFailed(ILogger logger, string pid, string errorMessage);
}
