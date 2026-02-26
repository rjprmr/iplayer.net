namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Abstraction for file system operations with built-in path safety validation (OWASP A01).
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Read all text from a file.
    /// </summary>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write text to a file, creating directories as needed.
    /// </summary>
    Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Append text to a file.
    /// </summary>
    Task AppendAllTextAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Check if a directory exists.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Create a directory and all parent directories.
    /// </summary>
    void CreateDirectory(string path);

    /// <summary>
    /// Delete a file if it exists.
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    /// Move/rename a file.
    /// </summary>
    void MoveFile(string sourcePath, string destinationPath, bool overwrite = false);

    /// <summary>
    /// Get the size of a file in bytes.
    /// </summary>
    long GetFileSize(string path);

    /// <summary>
    /// Get all files in a directory matching an optional search pattern.
    /// </summary>
    IEnumerable<string> GetFiles(string directory, string searchPattern = "*");

    /// <summary>
    /// Open a file for reading.
    /// </summary>
    Stream OpenRead(string path);

    /// <summary>
    /// Open a file for writing (creates or overwrites).
    /// </summary>
    Stream OpenWrite(string path);

    /// <summary>
    /// Safely combine and validate a path to prevent directory traversal attacks.
    /// Returns the resolved absolute path.
    /// </summary>
    /// <param name="basePath">The allowed base directory.</param>
    /// <param name="relativePath">The relative path to combine.</param>
    /// <returns>Resolved, validated absolute path.</returns>
    /// <exception cref="Exceptions.ValidationException">If the path escapes the base directory.</exception>
    string SafeCombinePath(string basePath, string relativePath);
}
