using GetIPlayer.Core.Exceptions;
using GetIPlayer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.FileSystem;

/// <summary>
/// File system abstraction with built-in path safety validation.
/// OWASP A01: Prevents directory traversal attacks via SafeCombinePath.
/// </summary>
public sealed partial class SafeFileSystem : IFileSystem
{
    private readonly ILogger<SafeFileSystem> _logger;

    public SafeFileSystem(ILogger<SafeFileSystem> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        ValidatePath(path);
        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        ValidatePath(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
    }

    public async Task AppendAllTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        ValidatePath(path);
        await File.AppendAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
    }

    public bool FileExists(string path)
    {
        ValidatePath(path);
        return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        ValidatePath(path);
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        ValidatePath(path);
        Directory.CreateDirectory(path);
    }

    public void DeleteFile(string path)
    {
        ValidatePath(path);
        if (File.Exists(path))
        {
            File.Delete(path);
            LogDeletedFile(_logger, path);
        }
    }

    public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        ValidatePath(sourcePath);
        ValidatePath(destinationPath);
        File.Move(sourcePath, destinationPath, overwrite);
        LogMovedFile(_logger, sourcePath, destinationPath);
    }

    public long GetFileSize(string path)
    {
        ValidatePath(path);
        return new FileInfo(path).Length;
    }

    public IEnumerable<string> GetFiles(string directory, string searchPattern = "*")
    {
        ValidatePath(directory);
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, searchPattern)
            : [];
    }

    public Stream OpenRead(string path)
    {
        ValidatePath(path);
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
    }

    public Stream OpenWrite(string path)
    {
        ValidatePath(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
    }

    /// <summary>
    /// Safely combine a base path with a relative path, preventing directory traversal.
    /// OWASP A01: This is the core defence against path traversal attacks.
    /// </summary>
    public string SafeCombinePath(string basePath, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var resolvedBase = Path.GetFullPath(basePath);
        var combined = Path.GetFullPath(Path.Combine(resolvedBase, relativePath));

        if (!combined.StartsWith(resolvedBase, StringComparison.OrdinalIgnoreCase))
        {
            LogPathTraversal(_logger, basePath, relativePath, combined);
            throw new ValidationException("path", $"Path traversal detected: the resolved path escapes the base directory.");
        }

        return combined;
    }

    /// <summary>
    /// Validates that a path doesn't contain null bytes or other dangerous patterns.
    /// </summary>
    private static void ValidatePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (path.Contains('\0'))
        {
            throw new ValidationException("path", "Path contains null bytes.");
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted file: {Path}")]
    private static partial void LogDeletedFile(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Moved file: {Source} -> {Destination}")]
    private static partial void LogMovedFile(ILogger logger, string source, string destination);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Path traversal attempt detected. Base: {Base}, Relative: {Relative}, Resolved: {Resolved}")]
    private static partial void LogPathTraversal(ILogger logger, string @base, string relative, string resolved);
}
