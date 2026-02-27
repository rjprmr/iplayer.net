using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Models;

/// <summary>
/// Represents a PVR (Personal Video Recorder) saved search.
/// </summary>
public sealed record PvrSearch
{
    /// <summary>Unique name for this PVR search.</summary>
    public required string Name { get; init; }

    /// <summary>Search term (regex pattern).</summary>
    public required string SearchTerm { get; init; }

    /// <summary>Programme type filter.</summary>
    public ProgrammeType? Type { get; init; }

    /// <summary>Channel regex filter.</summary>
    public string? ChannelFilter { get; init; }

    /// <summary>Quality preference list in priority order.</summary>
    public IReadOnlyList<QualityLevel> QualityPreferences { get; init; } = [];

    /// <summary>Whether subtitles should be downloaded.</summary>
    public bool DownloadSubtitles { get; init; }

    /// <summary>Whether this PVR search is enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Output directory override.</summary>
    public string? OutputDirectory { get; init; }

    /// <summary>When this search was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When this search last ran.</summary>
    public DateTimeOffset? LastRunAt { get; init; }
}
