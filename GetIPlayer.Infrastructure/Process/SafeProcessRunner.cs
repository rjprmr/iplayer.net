using System.Buffers;
using System.Diagnostics;
using GetIPlayer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Process;

/// <summary>
/// Safely executes external processes without shell injection.
/// OWASP A03: Uses Process.Start with UseShellExecute=false and array arguments.
/// Never passes arguments through a shell interpreter.
/// </summary>
public sealed partial class SafeProcessRunner : IExternalProcessRunner
{
    private static readonly SearchValues<char> ShellMetaChars = SearchValues.Create("|;&`$(){}[]!<>");
    private readonly ILogger<SafeProcessRunner> _logger;

    public SafeProcessRunner(ILogger<SafeProcessRunner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ValidateExecutablePath(executablePath);

        var startInfo = CreateStartInfo(executablePath, arguments, workingDirectory);

        var argsDisplay = string.Join(' ', arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
        LogRunningProcess(_logger, executablePath, argsDisplay);

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        LogProcessExited(_logger, process.ExitCode);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout,
            StandardError = stderr
        };
    }

    public async Task<int> RunWithOutputAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string> outputHandler,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ValidateExecutablePath(executablePath);

        var startInfo = CreateStartInfo(executablePath, arguments, workingDirectory);

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                outputHandler(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                outputHandler(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return process.ExitCode;
    }

    public string? FindExecutable(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        // If it's an absolute path, verify it exists
        if (Path.IsPathRooted(executablePath))
        {
            return File.Exists(executablePath) ? executablePath : null;
        }

        // Search in PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        var pathSeparator = OperatingSystem.IsWindows() ? ';' : ':';
        var extensions = OperatingSystem.IsWindows()
            ? new[] { ".exe", ".cmd", ".bat", "" }
            : new[] { "" };

        foreach (var directory in pathEnv.Split(pathSeparator))
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(directory, executablePath + ext);
                if (File.Exists(fullPath))
                {
                    return Path.GetFullPath(fullPath);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that the executable path is safe and exists.
    /// OWASP A03: Prevents injection through crafted executable paths.
    /// </summary>
    private void ValidateExecutablePath(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        // Block paths containing shell metacharacters
        if (executablePath.AsSpan().IndexOfAny(ShellMetaChars) >= 0)
        {
            throw new ArgumentException(
                $"Executable path contains disallowed characters: {executablePath}",
                nameof(executablePath));
        }

        var resolved = FindExecutable(executablePath);
        if (resolved is null)
        {
            LogExecutableNotFound(_logger, executablePath);
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string executablePath,
        IReadOnlyList<string> arguments,
        string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false, // OWASP A03: Never use shell execution
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        return startInfo;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Running process: {Executable} {Arguments}")]
    private static partial void LogRunningProcess(ILogger logger, string executable, string arguments);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Process exited with code {ExitCode}")]
    private static partial void LogProcessExited(ILogger logger, int exitCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Executable not found: {Path}")]
    private static partial void LogExecutableNotFound(ILogger logger, string path);
}
