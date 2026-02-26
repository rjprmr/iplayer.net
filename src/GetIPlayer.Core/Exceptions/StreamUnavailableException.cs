namespace GetIPlayer.Core.Exceptions;

/// <summary>
/// Thrown when no streams are available for a programme (e.g. all versions expired).
/// </summary>
public sealed class StreamUnavailableException : GetIPlayerException
{
    public string? Pid { get; }

    public StreamUnavailableException(string pid)
        : base($"No streams available for programme: {pid}")
    {
        Pid = pid;
    }

    public StreamUnavailableException(string pid, string message)
        : base(message)
    {
        Pid = pid;
    }

    public StreamUnavailableException(string pid, string message, Exception innerException)
        : base(message, innerException)
    {
        Pid = pid;
    }
}
