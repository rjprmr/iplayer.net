namespace GetIPlayer.Web.Services;

/// <summary>
/// Represents a queued download request from the web UI.
/// </summary>
public sealed record DownloadRequest
{
    public required string Id { get; init; }
    public required string Target { get; init; }
    public string? ConnectionId { get; init; }
    public bool Force { get; init; }
    public bool Subtitles { get; init; }
    public bool Overwrite { get; init; }
}
