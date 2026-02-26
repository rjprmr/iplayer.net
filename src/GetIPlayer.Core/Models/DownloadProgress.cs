namespace GetIPlayer.Core.Models;

/// <summary>
/// Represents the progress of a download operation.
/// </summary>
public sealed record DownloadProgress
{
    /// <summary>PID of the programme being downloaded.</summary>
    public required string Pid { get; init; }

    /// <summary>Total bytes expected (0 if unknown).</summary>
    public long TotalBytes { get; init; }

    /// <summary>Bytes downloaded so far.</summary>
    public long BytesDownloaded { get; init; }

    /// <summary>Download percentage (0-100). Null if total is unknown.</summary>
    public double? PercentComplete => TotalBytes > 0
        ? (double)BytesDownloaded / TotalBytes * 100
        : null;

    /// <summary>Current download speed in bytes per second.</summary>
    public double BytesPerSecond { get; init; }

    /// <summary>Estimated time remaining.</summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>Current phase of the download.</summary>
    public string Phase { get; init; } = string.Empty;
}
