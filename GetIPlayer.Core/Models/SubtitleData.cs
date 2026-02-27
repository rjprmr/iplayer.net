namespace GetIPlayer.Core.Models;

/// <summary>
/// Subtitle data for a programme.
/// </summary>
public sealed record SubtitleData
{
    /// <summary>PID of the programme.</summary>
    public required string Pid { get; init; }

    /// <summary>Subtitle content in SRT format.</summary>
    public required string SrtContent { get; init; }

    /// <summary>Original TTML content (if available).</summary>
    public string? TtmlContent { get; init; }

    /// <summary>Language code (e.g. "en").</summary>
    public string Language { get; init; } = "en";
}
