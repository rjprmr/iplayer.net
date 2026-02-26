namespace GetIPlayer.Core.Exceptions;

/// <summary>
/// Thrown when a download operation fails.
/// </summary>
public sealed class DownloadFailedException : GetIPlayerException
{
    public string? Pid { get; }

    public DownloadFailedException(string pid)
        : base($"Download failed for programme: {pid}")
    {
        Pid = pid;
    }

    public DownloadFailedException(string pid, string message)
        : base(message)
    {
        Pid = pid;
    }

    public DownloadFailedException(string pid, string message, Exception innerException)
        : base(message, innerException)
    {
        Pid = pid;
    }
}
