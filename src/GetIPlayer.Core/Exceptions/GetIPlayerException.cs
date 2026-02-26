namespace GetIPlayer.Core.Exceptions;

/// <summary>
/// Base exception for all GetIPlayer application errors.
/// </summary>
public class GetIPlayerException : Exception
{
    public GetIPlayerException() { }

    public GetIPlayerException(string message)
        : base(message) { }

    public GetIPlayerException(string message, Exception innerException)
        : base(message, innerException) { }
}
