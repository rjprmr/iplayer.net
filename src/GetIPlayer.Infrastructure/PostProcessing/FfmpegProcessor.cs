using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.PostProcessing;

/// <summary>
/// Post-processes media files using ffmpeg for remuxing, merging, and MP4 container creation.
/// OWASP A03: Uses safe process runner to prevent command injection.
/// </summary>
public sealed partial class FfmpegProcessor : IPostProcessor
{
    private readonly IExternalProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FfmpegProcessor> _logger;
    private readonly string _ffmpegPath;

    public FfmpegProcessor(
        IExternalProcessRunner processRunner,
        IFileSystem fileSystem,
        ILogger<FfmpegProcessor> logger,
        string? ffmpegPath = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ffmpegPath = ffmpegPath
            ?? _processRunner.FindExecutable("ffmpeg")
            ?? throw new InvalidOperationException("ffmpeg not found in PATH. Install ffmpeg or specify the path.");
    }

    /// <inheritdoc />
    public async Task RemuxAsync(
        string inputPath,
        string outputPath,
        string? subtitlePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        LogRemuxing(_logger, inputPath, outputPath);

        var args = new List<string>
        {
            "-y",          // Overwrite output
            "-i", inputPath,
        };

        if (!string.IsNullOrEmpty(subtitlePath) && _fileSystem.FileExists(subtitlePath))
        {
            args.AddRange(["-i", subtitlePath]);
        }

        args.AddRange([
            "-c", "copy",      // Copy streams without re-encoding
            "-movflags", "+faststart",  // Web-friendly MP4
            outputPath
        ]);

        var result = await _processRunner.RunAsync(
            _ffmpegPath, [.. args], cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            LogFfmpegError(_logger, result.ExitCode, result.StandardError);
            throw new InvalidOperationException(
                $"ffmpeg remux failed (exit code {result.ExitCode}): {result.StandardError}");
        }

        // Clean up input file
        _fileSystem.DeleteFile(inputPath);

        LogRemuxComplete(_logger, outputPath);
    }

    /// <inheritdoc />
    public async Task MergeAudioVideoAsync(
        string videoPath,
        string audioPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(audioPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        LogMerging(_logger, videoPath, audioPath, outputPath);

        var args = new[]
        {
            "-y",
            "-i", videoPath,
            "-i", audioPath,
            "-c", "copy",
            "-movflags", "+faststart",
            outputPath
        };

        var result = await _processRunner.RunAsync(
            _ffmpegPath, args, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            LogFfmpegError(_logger, result.ExitCode, result.StandardError);
            throw new InvalidOperationException(
                $"ffmpeg merge failed (exit code {result.ExitCode}): {result.StandardError}");
        }

        // Clean up temp files
        _fileSystem.DeleteFile(videoPath);
        _fileSystem.DeleteFile(audioPath);

        LogMergeComplete(_logger, outputPath);
    }

    /// <inheritdoc />
    public async Task TagAsync(
        string filePath,
        TagMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(metadata);

        LogTagging(_logger, filePath);

        var args = new List<string>
        {
            "-y",
            "-i", filePath,
            "-c", "copy"
        };

        AddMetadataArg(args, "title", metadata.Title);
        AddMetadataArg(args, "artist", metadata.Artist);
        AddMetadataArg(args, "album", metadata.Album);
        AddMetadataArg(args, "genre", metadata.Genre);
        AddMetadataArg(args, "comment", metadata.Description);
        AddMetadataArg(args, "copyright", metadata.Copyright);
        AddMetadataArg(args, "network", metadata.Network);

        if (metadata.Year > 0)
        {
            args.AddRange(["-metadata", $"date={metadata.Year}"]);
        }

        var tempPath = filePath + ".tag.tmp";
        args.Add(tempPath);

        var result = await _processRunner.RunAsync(
            _ffmpegPath, [.. args], cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ExitCode == 0 && _fileSystem.FileExists(tempPath))
        {
            _fileSystem.DeleteFile(filePath);
            _fileSystem.MoveFile(tempPath, filePath);
            LogTagComplete(_logger, filePath);
        }
        else
        {
            if (_fileSystem.FileExists(tempPath))
            {
                _fileSystem.DeleteFile(tempPath);
            }

            LogTagFailed(_logger, filePath, result.StandardError);
        }
    }

    private static void AddMetadataArg(List<string> args, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            args.AddRange(["-metadata", $"{key}={value}"]);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Remuxing {InputPath} to {OutputPath}")]
    private static partial void LogRemuxing(ILogger logger, string inputPath, string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Remux complete: {OutputPath}")]
    private static partial void LogRemuxComplete(ILogger logger, string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Merging {VideoPath} + {AudioPath} to {OutputPath}")]
    private static partial void LogMerging(ILogger logger, string videoPath, string audioPath, string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Merge complete: {OutputPath}")]
    private static partial void LogMergeComplete(ILogger logger, string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tagging {FilePath}")]
    private static partial void LogTagging(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tagging complete: {FilePath}")]
    private static partial void LogTagComplete(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tagging failed for {FilePath}: {ErrorMessage}")]
    private static partial void LogTagFailed(ILogger logger, string filePath, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "ffmpeg error (exit {ExitCode}): {ErrorOutput}")]
    private static partial void LogFfmpegError(ILogger logger, int exitCode, string errorOutput);
}
