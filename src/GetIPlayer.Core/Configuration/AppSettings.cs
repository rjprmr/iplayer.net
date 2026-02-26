using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Configuration;

/// <summary>
/// Root configuration for the GetIPlayer application.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string SectionName = "GetIPlayer";

    /// <summary>Profile directory path (stores cache, history, PVR, options).</summary>
    public string ProfileDir { get; set; } = string.Empty;

    /// <summary>Default programme types to search.</summary>
    public ProgrammeType[] DefaultTypes { get; set; } = [ProgrammeType.Tv];

    /// <summary>Maximum number of retry attempts for HTTP requests.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Whether to check for new releases at startup.</summary>
    public bool CheckForUpdates { get; set; }

    /// <summary>Download options.</summary>
    public DownloadOptions Download { get; set; } = new();

    /// <summary>Proxy settings.</summary>
    public ProxySettings Proxy { get; set; } = new();

    /// <summary>Output file options.</summary>
    public OutputOptions Output { get; set; } = new();
}
