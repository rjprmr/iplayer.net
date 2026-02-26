using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Models;

/// <summary>
/// Represents a BBC iPlayer or BBC Sounds programme.
/// </summary>
public sealed record Programme
{
    /// <summary>The unique BBC programme identifier (PID), e.g. "b01rryzz".</summary>
    public required string Pid { get; init; }

    /// <summary>Programme type (TV or Radio).</summary>
    public required ProgrammeType Type { get; init; }

    /// <summary>Programme series/brand name.</summary>
    public required string Name { get; init; }

    /// <summary>Episode title.</summary>
    public string Episode { get; init; } = string.Empty;

    /// <summary>Full description of the episode.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Broadcasting channel name (e.g. "BBC One", "Radio 4").</summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>URL to the programme's thumbnail image.</summary>
    public string ThumbnailUrl { get; init; } = string.Empty;

    /// <summary>URL to the programme on iPlayer/Sounds website.</summary>
    public string WebUrl { get; init; } = string.Empty;

    /// <summary>Duration of the programme.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>When the programme was first broadcast.</summary>
    public DateTimeOffset? FirstBroadcast { get; init; }

    /// <summary>When the programme becomes available on iPlayer/Sounds.</summary>
    public DateTimeOffset? AvailableFrom { get; init; }

    /// <summary>When the programme expires from iPlayer/Sounds.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Available versions (e.g. "original", "signed", "audiodescribed").</summary>
    public IReadOnlyList<StreamVersion> Versions { get; init; } = [];

    /// <summary>Numeric index for CLI display.</summary>
    public int Index { get; init; }

    /// <summary>Whether the programme is complete (all metadata loaded).</summary>
    public bool IsComplete { get; init; }
}
