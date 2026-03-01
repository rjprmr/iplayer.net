namespace GetIPlayer.Infrastructure.Bbc;

/// <summary>
/// Constructs BBC API URLs safely.
/// OWASP A10: Uses only known BBC domain endpoints.
/// </summary>
public static class UrlBuilder
{
    private const string BbcBaseUrl = "https://www.bbc.co.uk";

    /// <summary>
    /// Build the URL for a programme's playlist JSON (used for version/stream discovery).
    /// </summary>
    public static string PlaylistJson(string pid)
    {
        PidValidator.EnsureValid(pid);
        return $"{BbcBaseUrl}/programmes/{Uri.EscapeDataString(pid)}/playlist.json";
    }

    /// <summary>
    /// Build the URL for a programme's page (for metadata scraping).
    /// </summary>
    public static string ProgrammePage(string pid)
    {
        PidValidator.EnsureValid(pid);
        return $"{BbcBaseUrl}/programmes/{Uri.EscapeDataString(pid)}";
    }

    /// <summary>
    /// Build the URL for a channel's schedule in JSON format.
    /// </summary>
    public static string ScheduleJson(string channelId, DateOnly date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var safeChannelId = Uri.EscapeDataString(channelId);
        return $"{BbcBaseUrl}/{safeChannelId}/programmes/schedules/{date:yyyy/MM/dd}.json";
    }

    /// <summary>
    /// Build the EpisodeGuide URL for a programme PID.
    /// </summary>
    public static string EpisodeGuide(string pid)
    {
        PidValidator.EnsureValid(pid);
        return $"{BbcBaseUrl}/programmes/{Uri.EscapeDataString(pid)}/episodes/player";
    }

    /// <summary>
    /// Build the iPlayer episode URL.
    /// </summary>
    public static string IPlayerEpisode(string pid)
    {
        PidValidator.EnsureValid(pid);
        return $"https://www.bbc.co.uk/iplayer/episode/{Uri.EscapeDataString(pid)}";
    }

    /// <summary>
    /// Build the BBC Sounds play URL.
    /// </summary>
    public static string SoundsPlay(string pid)
    {
        PidValidator.EnsureValid(pid);
        return $"https://www.bbc.co.uk/sounds/play/{Uri.EscapeDataString(pid)}";
    }

    /// <summary>
    /// Build the schedule URL without a specific date (defaults to today).
    /// </summary>
    public static string ScheduleJson(string channelId)
    {
        return ScheduleJson(channelId, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>
    /// Build the BBC iPlayer search API URL (ibl JSON API).
    /// </summary>
    public static string Search(string query, Core.Enums.ProgrammeType type, int page = 1, int pageSize = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var safeQuery = Uri.EscapeDataString(query);
        var mediaType = type == Core.Enums.ProgrammeType.Tv ? "tv" : "radio";
        return $"https://ibl.api.bbc.co.uk/ibl/v1/search?q={safeQuery}&mediatype={mediaType}&page={page}&pagesize={pageSize}";
    }

    /// <summary>
    /// Build the BBC ibl channels API URL.
    /// </summary>
    public static string Channels(Core.Enums.ProgrammeType type)
    {
        var kind = type == Core.Enums.ProgrammeType.Tv ? "tv" : "radio";
        return $"https://ibl.api.bbc.co.uk/ibl/v1/channels?kind={kind}";
    }
}
