using System.Text.Json;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Exceptions;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Bbc;

/// <summary>
/// Discovers and resolves available media streams for BBC programmes.
/// </summary>
public sealed partial class BbcStreamService : IStreamService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<BbcStreamService> _logger;

    public BbcStreamService(
        IHttpClientService httpClient,
        ILogger<BbcStreamService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<StreamInfo>> GetStreamsAsync(
        string pid,
        QualityLevel[] qualityPreferences,
        string[] versions,
        CancellationToken cancellationToken = default)
    {
        PidValidator.EnsureValid(pid);

        var versionPids = await GetVersionsAsync(pid, cancellationToken).ConfigureAwait(false);
        if (versionPids.Count == 0)
        {
            throw new StreamUnavailableException(pid, $"No versions found for programme {pid}");
        }

        var allStreams = new List<StreamInfo>();
        foreach (var version in versionPids)
        {
            if (versions.Length > 0 && !versions.Contains(version.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            allStreams.AddRange(version.Streams);
        }

        // Sort by quality preference order
        var qualityOrder = qualityPreferences
            .Select((q, i) => (Quality: q, Order: i))
            .ToDictionary(x => x.Quality, x => x.Order);

        return allStreams
            .OrderBy(s => qualityOrder.TryGetValue(s.Quality, out var order) ? order : int.MaxValue)
            .ThenByDescending(s => s.Bitrate)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<StreamVersion>> GetVersionsAsync(
        string pid,
        CancellationToken cancellationToken = default)
    {
        PidValidator.EnsureValid(pid);

        var url = UrlBuilder.PlaylistJson(pid);
        LogFetchingVersions(_logger, pid, url);

        string json;
        try
        {
            json = await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogVersionFetchFailed(_logger, pid, ex.Message);
            throw new StreamUnavailableException(pid, $"Failed to fetch versions for {pid}", ex);
        }

        return ParseVersionsFromPlaylist(json, pid);
    }

    private List<StreamVersion> ParseVersionsFromPlaylist(string json, string pid)
    {
        var versions = new List<StreamVersion>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("allAvailableVersions", out var availableVersions) ||
                availableVersions.ValueKind != JsonValueKind.Array)
            {
                return versions;
            }

            foreach (var versionElement in availableVersions.EnumerateArray())
            {
                var versionPid = versionElement.GetPropertyOrDefault("pid");
                var versionName = versionElement.GetNestedPropertyOrDefault("types", "type") ?? "original";

                if (string.IsNullOrEmpty(versionPid))
                {
                    continue;
                }

                var streams = new List<StreamInfo>();

                // Extract available media sets
                if (versionElement.TryGetProperty("smpConfig", out var smpConfig) &&
                    smpConfig.TryGetProperty("items", out var items) &&
                    items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (!item.TryGetProperty("connection", out var connections) ||
                            connections.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var conn in connections.EnumerateArray())
                        {
                            var streamInfo = ParseConnection(conn);
                            if (streamInfo is not null)
                            {
                                streams.Add(streamInfo);
                            }
                        }
                    }
                }

                var duration = TimeSpan.Zero;
                if (versionElement.TryGetProperty("duration", out var durationElem) &&
                    durationElem.TryGetInt32(out var durationSecs))
                {
                    duration = TimeSpan.FromSeconds(durationSecs);
                }

                versions.Add(new StreamVersion
                {
                    VersionPid = versionPid,
                    Name = versionName,
                    Duration = duration,
                    Streams = streams.AsReadOnly()
                });
            }
        }
        catch (JsonException ex)
        {
            LogPlaylistParseError(_logger, pid, ex.Message);
        }

        return versions;
    }

    private static StreamInfo? ParseConnection(JsonElement connection)
    {
        var href = connection.GetPropertyOrDefault("href");
        if (string.IsNullOrEmpty(href) || !Uri.TryCreate(href, UriKind.Absolute, out var streamUri))
        {
            return null;
        }

        var protocol = href.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
            ? StreamProtocol.Hls
            : href.Contains(".mpd", StringComparison.OrdinalIgnoreCase)
                ? StreamProtocol.Dash
                : StreamProtocol.Hls; // default

        var supplier = connection.GetPropertyOrDefault("supplier") ?? string.Empty;
        var transferFormat = connection.GetPropertyOrDefault("transferFormat") ?? string.Empty;

        // Parse quality from known patterns
        var quality = DetermineQuality(href);

        return new StreamInfo
        {
            Mode = $"{transferFormat}_{supplier}_{quality}".ToLowerInvariant(),
            Protocol = protocol,
            Quality = quality,
            StreamUrl = streamUri,
            Supplier = supplier
        };
    }

    private static QualityLevel DetermineQuality(string url)
    {
        if (url.Contains("hd", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("1280", StringComparison.OrdinalIgnoreCase))
        {
            return QualityLevel.Hd;
        }

        if (url.Contains("sd", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("704", StringComparison.OrdinalIgnoreCase))
        {
            return QualityLevel.Sd;
        }

        if (url.Contains("web", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("512", StringComparison.OrdinalIgnoreCase))
        {
            return QualityLevel.Web;
        }

        return QualityLevel.Mobile;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching versions for {Pid} from {Url}")]
    private static partial void LogFetchingVersions(ILogger logger, string pid, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch versions for {Pid}: {ErrorMessage}")]
    private static partial void LogVersionFetchFailed(ILogger logger, string pid, string errorMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse playlist JSON for {Pid}: {ErrorMessage}")]
    private static partial void LogPlaylistParseError(ILogger logger, string pid, string errorMessage);
}
