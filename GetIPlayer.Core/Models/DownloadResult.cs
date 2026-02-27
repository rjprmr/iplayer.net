using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Models;

/// <summary>
/// Result of a download operation.
/// </summary>
public sealed record DownloadResult
{
    /// <summary>PID of the programme that was downloaded.</summary>
    public required string Pid { get; init; }

    /// <summary>Status of the download.</summary>
    public required DownloadStatus Status { get; init; }

    /// <summary>File path of the downloaded file (if successful).</summary>
    public string? FilePath { get; init; }

    /// <summary>Mode used for the download (e.g. "hvf_hls_hd").</summary>
    public string? Mode { get; init; }

    /// <summary>Total bytes downloaded.</summary>
    public long BytesDownloaded { get; init; }

    /// <summary>Error message if the download failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Duration of the download operation.</summary>
    public TimeSpan Duration { get; init; }
}
