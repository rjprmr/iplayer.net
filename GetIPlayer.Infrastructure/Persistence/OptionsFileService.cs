using System.Text.Json;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Persistence;

/// <summary>
/// Reads and writes application preferences/options from a JSON file.
/// Replaces the original Perl options file format.
/// </summary>
public sealed partial class OptionsFileService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<OptionsFileService> _logger;
    private readonly string _optionsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public OptionsFileService(
        IFileSystem fileSystem,
        ILogger<OptionsFileService> logger,
        string optionsPath)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsPath = optionsPath ?? throw new ArgumentNullException(nameof(optionsPath));
    }

    /// <summary>
    /// Load application settings from the options file.
    /// Returns default settings if the file does not exist.
    /// </summary>
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.FileExists(_optionsPath))
        {
            LogNoOptionsFile(_logger, _optionsPath);
            return new AppSettings();
        }

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(_optionsPath, cancellationToken).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            LogOptionsLoaded(_logger, _optionsPath);
            return settings ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            LogOptionsCorrupt(_logger, _optionsPath, ex.Message);
            return new AppSettings();
        }
    }

    /// <summary>
    /// Save application settings to the options file.
    /// </summary>
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var dir = Path.GetDirectoryName(_optionsPath);
        if (!string.IsNullOrEmpty(dir))
        {
            _fileSystem.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await _fileSystem.WriteAllTextAsync(_optionsPath, json, cancellationToken).ConfigureAwait(false);
        LogOptionsSaved(_logger, _optionsPath);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No options file found at {Path}, using defaults")]
    private static partial void LogNoOptionsFile(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Options loaded from {Path}")]
    private static partial void LogOptionsLoaded(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Options file corrupt at {Path}: {ErrorMessage}")]
    private static partial void LogOptionsCorrupt(ILogger logger, string path, string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Options saved to {Path}")]
    private static partial void LogOptionsSaved(ILogger logger, string path);
}
