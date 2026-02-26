using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Models;

/// <summary>
/// A record in the download history.
/// </summary>
public sealed record HistoryRecord
{
    /// <summary>Programme PID.</summary>
    public required string Pid { get; init; }

    /// <summary>Programme name.</summary>
    public required string Name { get; init; }

    /// <summary>Episode title.</summary>
    public string Episode { get; init; } = string.Empty;

    /// <summary>Programme type.</summary>
    public ProgrammeType Type { get; init; }

    /// <summary>When this programme was downloaded.</summary>
    public DateTimeOffset DownloadedAt { get; init; }

    /// <summary>Download mode used (e.g. "hvf_hls_hd").</summary>
    public string Mode { get; init; } = string.Empty;

    /// <summary>Path to the downloaded file.</summary>
    public string? FilePath { get; init; }

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; init; }

    /// <summary>Channel the programme was broadcast on.</summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>Version downloaded (e.g. "original").</summary>
    public string Version { get; init; } = "original";

    /// <summary>Duration of the programme.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Thumbnail URL.</summary>
    public string? ThumbnailUrl { get; init; }
}
