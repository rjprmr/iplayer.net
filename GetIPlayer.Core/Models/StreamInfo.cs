using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Models;

/// <summary>
/// Metadata about an available stream for a programme version.
/// </summary>
public sealed record StreamInfo
{
    /// <summary>Stream mode identifier (e.g. "hvf_hls_hd", "dash_hd").</summary>
    public required string Mode { get; init; }

    /// <summary>Streaming protocol used.</summary>
    public required StreamProtocol Protocol { get; init; }

    /// <summary>Quality level of this stream.</summary>
    public required QualityLevel Quality { get; init; }

    /// <summary>Stream URL (playlist/manifest).</summary>
    public required Uri StreamUrl { get; init; }

    /// <summary>Bitrate in kbps.</summary>
    public int Bitrate { get; init; }

    /// <summary>Video width in pixels (0 for audio-only).</summary>
    public int Width { get; init; }

    /// <summary>Video height in pixels (0 for audio-only).</summary>
    public int Height { get; init; }

    /// <summary>Frame rate (e.g. 25, 50).</summary>
    public double FrameRate { get; init; }

    /// <summary>Media type (e.g. "video", "audio").</summary>
    public string MediaType { get; init; } = string.Empty;

    /// <summary>CDN supplier name.</summary>
    public string Supplier { get; init; } = string.Empty;

    /// <summary>Whether this is an audio+video combined stream or separate.</summary>
    public bool IsMuxed { get; init; }
}
