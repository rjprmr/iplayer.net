namespace GetIPlayer.Core.Models;

/// <summary>
/// Metadata to be written to an MP4/M4A file by a tagger (e.g. AtomicParsley).
/// </summary>
public sealed record TagMetadata
{
    /// <summary>Programme/show title.</summary>
    public required string Title { get; init; }

    /// <summary>Artist or channel name.</summary>
    public string Artist { get; init; } = string.Empty;

    /// <summary>Album or series name.</summary>
    public string Album { get; init; } = string.Empty;

    /// <summary>Genre.</summary>
    public string Genre { get; init; } = string.Empty;

    /// <summary>Episode description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Long description / synopsis.</summary>
    public string LongDescription { get; init; } = string.Empty;

    /// <summary>Track number within series.</summary>
    public int? TrackNumber { get; init; }

    /// <summary>Total tracks in series.</summary>
    public int? TrackTotal { get; init; }

    /// <summary>Year of broadcast.</summary>
    public int? Year { get; init; }

    /// <summary>Network / channel name.</summary>
    public string Network { get; init; } = string.Empty;

    /// <summary>Path to thumbnail image to embed as artwork.</summary>
    public string? ThumbnailPath { get; init; }

    /// <summary>Copyright notice.</summary>
    public string Copyright { get; init; } = string.Empty;

    /// <summary>Comment field.</summary>
    public string Comment { get; init; } = string.Empty;

    /// <summary>Whether this is a podcast (for M4A media kind).</summary>
    public bool IsPodcast { get; init; }

    /// <summary>Category for the programme.</summary>
    public string Category { get; init; } = string.Empty;
}
