namespace GetIPlayer.Core.Models;

/// <summary>
/// Represents a BBC channel.
/// </summary>
public sealed record ChannelInfo
{
    /// <summary>Channel identifier used in BBC URLs.</summary>
    public required string Id { get; init; }

    /// <summary>Display name (e.g. "BBC One", "Radio 4").</summary>
    public required string Name { get; init; }

    /// <summary>Whether this is a TV or radio channel.</summary>
    public required Enums.ProgrammeType Type { get; init; }
}
