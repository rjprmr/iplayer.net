namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Safely executes external processes (ffmpeg, AtomicParsley) without shell injection.
/// </summary>
public interface IExternalProcessRunner
{
    /// <summary>
    /// Run an external process and capture its output.
    /// </summary>
    /// <param name="executablePath">Absolute path to the executable.</param>
    /// <param name="arguments">Arguments as separate strings (never shell-concatenated).</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (exitCode, standardOutput, standardError).</returns>
    Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run an external process and stream output line-by-line.
    /// </summary>
    /// <param name="executablePath">Absolute path to the executable.</param>
    /// <param name="arguments">Arguments as separate strings.</param>
    /// <param name="outputHandler">Handler called for each line of output.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code of the process.</returns>
    Task<int> RunWithOutputAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string> outputHandler,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an executable exists and is accessible.
    /// </summary>
    /// <param name="executablePath">Path or name of the executable.</param>
    /// <returns>The resolved absolute path, or null if not found.</returns>
    string? FindExecutable(string executablePath);
}

/// <summary>
/// Result of an external process execution.
/// </summary>
public sealed record ProcessResult
{
    /// <summary>Process exit code.</summary>
    public required int ExitCode { get; init; }

    /// <summary>Standard output content.</summary>
    public required string StandardOutput { get; init; }

    /// <summary>Standard error content.</summary>
    public required string StandardError { get; init; }

    /// <summary>Whether the process completed successfully (exit code 0).</summary>
    public bool Success => ExitCode == 0;
}
