namespace GetIPlayer.Core.Enums;

/// <summary>
/// Status of a download operation.
/// </summary>
public enum DownloadStatus
{
    /// <summary>Download completed successfully.</summary>
    Success,

    /// <summary>Download failed due to an error.</summary>
    Failed,

    /// <summary>Download was skipped (already in history or filtered out).</summary>
    Skipped,

    /// <summary>Programme is not available (expired or geo-blocked).</summary>
    Unavailable,

    /// <summary>Download is currently in progress.</summary>
    InProgress,

    /// <summary>Download was cancelled by the user.</summary>
    Cancelled,

    /// <summary>Download is queued for later.</summary>
    Queued
}
