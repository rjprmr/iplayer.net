namespace GetIPlayer.Core.Exceptions;

/// <summary>
/// Thrown when a programme cannot be found by PID or search.
/// </summary>
public sealed class ProgrammeNotFoundException : GetIPlayerException
{
    public string? Pid { get; }

    public ProgrammeNotFoundException(string pid)
        : base($"Programme not found: {pid}")
    {
        Pid = pid;
    }

    public ProgrammeNotFoundException(string pid, string message)
        : base(message)
    {
        Pid = pid;
    }

    public ProgrammeNotFoundException(string pid, string message, Exception innerException)
        : base(message, innerException)
    {
        Pid = pid;
    }
}
