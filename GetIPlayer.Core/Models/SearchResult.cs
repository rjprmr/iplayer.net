namespace GetIPlayer.Core.Models;

/// <summary>
/// Wrapper for paginated search results.
/// </summary>
public sealed record SearchResult
{
    /// <summary>The matching programmes.</summary>
    public required IReadOnlyList<Programme> Programmes { get; init; }

    /// <summary>Total number of matches (before pagination).</summary>
    public int TotalCount { get; init; }

    /// <summary>Current page (1-based).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Page size used.</summary>
    public int PageSize { get; init; } = 30;
}
