namespace GetIPlayer.Core.Configuration;

/// <summary>
/// Output file naming and directory configuration.
/// </summary>
public sealed class OutputOptions
{
    /// <summary>
    /// Default output directory for downloads.
    /// </summary>
    public string OutputDir { get; set; } = string.Empty;

    /// <summary>
    /// File prefix format template. Supports placeholders like {name}, {episode}, {pid}, {channel}.
    /// </summary>
    public string FilePrefix { get; set; } = "{name} - {episode} {pid}";

    /// <summary>
    /// Subdirectory format template. Supports the same placeholders as FilePrefix.
    /// </summary>
    public string SubDir { get; set; } = string.Empty;

    /// <summary>
    /// Whether to add version name to filename when not "original".
    /// </summary>
    public bool AddVersionToFilename { get; set; } = true;

    /// <summary>
    /// Maximum filename length (excluding extension).
    /// </summary>
    public int MaxFilenameLength { get; set; } = 200;
}
