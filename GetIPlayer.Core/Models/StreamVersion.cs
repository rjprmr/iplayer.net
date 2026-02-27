namespace GetIPlayer.Core.Models;

/// <summary>
/// Represents a programme version (e.g. "original", "signed", "audiodescribed").
/// </summary>
public sealed record StreamVersion
{
    /// <summary>Version PID.</summary>
    public required string VersionPid { get; init; }

    /// <summary>Version name (e.g. "original", "signed", "audiodescribed").</summary>
    public required string Name { get; init; }

    /// <summary>Duration of this version.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Available stream options for this version.</summary>
    public IReadOnlyList<StreamInfo> Streams { get; init; } = [];
}
