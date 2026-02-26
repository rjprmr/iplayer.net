using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Configuration;

/// <summary>
/// Download-specific configuration options.
/// </summary>
public sealed class DownloadOptions
{
    /// <summary>TV quality preferences in priority order.</summary>
    public QualityLevel[] TvQuality { get; set; } = [QualityLevel.Hd, QualityLevel.Sd, QualityLevel.Web, QualityLevel.Mobile];

    /// <summary>Radio quality preferences in priority order.</summary>
    public QualityLevel[] RadioQuality { get; set; } = [QualityLevel.Hd, QualityLevel.Sd, QualityLevel.Web, QualityLevel.Mobile];

    /// <summary>Whether to download subtitles by default.</summary>
    public bool Subtitles { get; set; }

    /// <summary>Whether to download the programme thumbnail.</summary>
    public bool Thumbnail { get; set; } = true;

    /// <summary>Whether to tag the downloaded file with metadata.</summary>
    public bool Tag { get; set; } = true;

    /// <summary>Whether to overwrite existing files.</summary>
    public bool Overwrite { get; set; }

    /// <summary>Whether to force re-download even if in history.</summary>
    public bool Force { get; set; }

    /// <summary>Maximum concurrent segment downloads.</summary>
    public int MaxConcurrentSegments { get; set; } = 4;

    /// <summary>Programme versions to attempt, in order (e.g. "original", "signed").</summary>
    public string[] Versions { get; set; } = ["original"];

    /// <summary>Path to ffmpeg binary.</summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>Path to AtomicParsley binary.</summary>
    public string AtomicParsleyPath { get; set; } = "AtomicParsley";
}
