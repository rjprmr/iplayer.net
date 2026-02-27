namespace GetIPlayer.Core.Exceptions;

/// <summary>
/// Thrown when a programme is geo-blocked and not available in the user's region.
/// </summary>
public sealed class GeoBlockedException : GetIPlayerException
{
    public string? Pid { get; }

    public GeoBlockedException(string pid)
        : base($"Programme is geo-blocked and not available in your region: {pid}")
    {
        Pid = pid;
    }

    public GeoBlockedException(string pid, string message)
        : base(message)
    {
        Pid = pid;
    }

    public GeoBlockedException(string pid, string message, Exception innerException)
        : base(message, innerException)
    {
        Pid = pid;
    }
}
