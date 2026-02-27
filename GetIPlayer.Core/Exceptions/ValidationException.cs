namespace GetIPlayer.Core.Exceptions;

/// <summary>
/// Thrown when input validation fails (e.g. invalid PID format, unsafe paths).
/// </summary>
public sealed class ValidationException : GetIPlayerException
{
    /// <summary>The name of the field or parameter that failed validation.</summary>
    public string? FieldName { get; }

    public ValidationException(string message)
        : base(message) { }

    public ValidationException(string fieldName, string message)
        : base(message)
    {
        FieldName = fieldName;
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
