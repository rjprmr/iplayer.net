using System.Text.Json;
using System.Text.RegularExpressions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Exceptions;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Bbc;

/// <summary>
/// Retrieves programme metadata and search results from the BBC APIs.
/// </summary>
public sealed partial class BbcProgrammeService : IProgrammeService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<BbcProgrammeService> _logger;

    public BbcProgrammeService(
        IHttpClientService httpClient,
        ILogger<BbcProgrammeService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Programme?> GetByPidAsync(string pid, CancellationToken cancellationToken = default)
    {
        PidValidator.EnsureValid(pid);

        var url = UrlBuilder.ProgrammePage(pid);
        LogFetchingProgramme(_logger, pid, url);

        string json;
        try
        {
            json = await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new GeoBlockedException(pid);
        }

        return MetadataParser.ParseProgramme(json);
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(
        string searchTerm,
        ProgrammeType[] types,
        string? channelFilter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        var type = types.Length > 0 ? types[0] : ProgrammeType.Tv;
        var url = UrlBuilder.Search(searchTerm, type);
        LogSearching(_logger, searchTerm, type, url);

        string json;
        try
        {
            json = await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogSearchFailed(_logger, searchTerm, ex.Message);
            return new SearchResult { Programmes = [], TotalCount = 0, Page = 1, PageSize = 25 };
        }

        var result = ParseSearchResults(json, 1, 25, type);

        // Apply channel filter if specified
        if (!string.IsNullOrEmpty(channelFilter))
        {
            var filtered = result.Programmes
                .Where(p => p.Channel.Contains(channelFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
            result = result with { Programmes = filtered.AsReadOnly(), TotalCount = filtered.Count };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChannelInfo>> GetChannelsAsync(
        ProgrammeType type,
        CancellationToken cancellationToken = default)
    {
        var url = UrlBuilder.ScheduleJson(type == ProgrammeType.Tv ? "bbc_one" : "bbc_radio_one");
        LogFetchingChannels(_logger, type);

        // Return static well-known channel list for reliability
        return type == ProgrammeType.Tv
            ? GetTvChannels()
            : GetRadioChannels();
    }

    private SearchResult ParseSearchResults(string json, int page, int pageSize, ProgrammeType type)
    {
        var programmes = new List<Programme>();
        var totalCount = 0;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("results", out var results) &&
                results.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in results.EnumerateArray())
                {
                    var programme = MetadataParser.ParseProgrammeElement(element, type);
                    if (programme is not null)
                    {
                        programmes.Add(programme);
                    }
                }
            }

            if (root.TryGetProperty("count", out var countElem) &&
                countElem.TryGetInt32(out var count))
            {
                totalCount = count;
            }
            else
            {
                totalCount = programmes.Count;
            }
        }
        catch (JsonException ex)
        {
            LogSearchParseError(_logger, ex.Message);
        }

        return new SearchResult
        {
            Programmes = programmes.AsReadOnly(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IReadOnlyList<ChannelInfo> GetTvChannels() =>
    [
        new() { Id = "bbc_one", Name = "BBC One", Type = ProgrammeType.Tv },
        new() { Id = "bbc_two", Name = "BBC Two", Type = ProgrammeType.Tv },
        new() { Id = "bbc_three", Name = "BBC Three", Type = ProgrammeType.Tv },
        new() { Id = "bbc_four", Name = "BBC Four", Type = ProgrammeType.Tv },
        new() { Id = "cbbc", Name = "CBBC", Type = ProgrammeType.Tv },
        new() { Id = "cbeebies", Name = "CBeebies", Type = ProgrammeType.Tv },
        new() { Id = "bbc_news24", Name = "BBC News", Type = ProgrammeType.Tv },
        new() { Id = "bbc_parliament", Name = "BBC Parliament", Type = ProgrammeType.Tv },
        new() { Id = "bbc_alba", Name = "BBC Alba", Type = ProgrammeType.Tv },
        new() { Id = "s4c", Name = "S4C", Type = ProgrammeType.Tv },
    ];

    private static IReadOnlyList<ChannelInfo> GetRadioChannels() =>
    [
        new() { Id = "bbc_radio_one", Name = "BBC Radio 1", Type = ProgrammeType.Radio },
        new() { Id = "bbc_radio_two", Name = "BBC Radio 2", Type = ProgrammeType.Radio },
        new() { Id = "bbc_radio_three", Name = "BBC Radio 3", Type = ProgrammeType.Radio },
        new() { Id = "bbc_radio_fourfm", Name = "BBC Radio 4", Type = ProgrammeType.Radio },
        new() { Id = "bbc_radio_five_live", Name = "BBC Radio 5 Live", Type = ProgrammeType.Radio },
        new() { Id = "bbc_6music", Name = "BBC Radio 6 Music", Type = ProgrammeType.Radio },
        new() { Id = "bbc_asian_network", Name = "BBC Asian Network", Type = ProgrammeType.Radio },
        new() { Id = "bbc_world_service", Name = "BBC World Service", Type = ProgrammeType.Radio },
        new() { Id = "bbc_radio_1xtra", Name = "BBC Radio 1Xtra", Type = ProgrammeType.Radio },
        new() { Id = "bbc_radio_four_extra", Name = "BBC Radio 4 Extra", Type = ProgrammeType.Radio },
    ];

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching programme {Pid} from {Url}")]
    private static partial void LogFetchingProgramme(ILogger logger, string pid, string url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Searching for '{Query}' type={Type} from {Url}")]
    private static partial void LogSearching(ILogger logger, string query, ProgrammeType type, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Search failed for '{Query}': {ErrorMessage}")]
    private static partial void LogSearchFailed(ILogger logger, string query, string errorMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse search results: {ErrorMessage}")]
    private static partial void LogSearchParseError(ILogger logger, string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching channels for type {Type}")]
    private static partial void LogFetchingChannels(ILogger logger, ProgrammeType type);
}
