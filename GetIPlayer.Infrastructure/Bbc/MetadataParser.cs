using System.Text.Json;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.Bbc;

/// <summary>
/// Parses BBC programme metadata from JSON API responses.
/// </summary>
public sealed partial class MetadataParser
{
    private readonly ILogger<MetadataParser> _logger;

    public MetadataParser(ILogger<MetadataParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parse programme metadata from a BBC programmes JSON response.
    /// </summary>
    public Programme? ParseProgrammeJson(string json, ProgrammeType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("programme", out var prog))
            {
                LogNoProgrammeElement(_logger);
                return null;
            }

            return ParseProgrammeElement(prog, type);
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Parse programme metadata from a JSON string containing a top-level programme object.
    /// </summary>
    public static Programme? ParseProgramme(string json, ProgrammeType type = ProgrammeType.Tv)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("programme", out var prog))
            {
                return ParseProgrammeElement(prog, type);
            }

            // Attempt to parse the root element directly
            return ParseProgrammeElement(root, type);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parse a programme element from BBC JSON.
    /// </summary>
    public static Programme? ParseProgrammeElement(JsonElement element, ProgrammeType type)
    {
        var pid = element.GetPropertyOrDefault("pid");
        if (string.IsNullOrEmpty(pid))
        {
            return null;
        }

        var title = element.GetPropertyOrDefault("title") ?? string.Empty;
        var displayTitle = element.GetNestedPropertyOrDefault("display_title", "title") ?? title;
        var subtitle = element.GetNestedPropertyOrDefault("display_title", "subtitle") ?? string.Empty;

        var shortSynopsis = element.GetPropertyOrDefault("short_synopsis") ?? string.Empty;
        var mediumSynopsis = element.GetPropertyOrDefault("medium_synopsis") ?? shortSynopsis;

        var channel = element.GetNestedPropertyOrDefault("ownership", "service", "title") ?? string.Empty;

        var thumbnailUrl = string.Empty;
        if (element.TryGetProperty("image", out var image))
        {
            var templateUrl = image.GetPropertyOrDefault("pid");
            if (!string.IsNullOrEmpty(templateUrl))
            {
                thumbnailUrl = $"https://ichef.bbci.co.uk/images/ic/480x270/{templateUrl}.jpg";
            }
        }

        var duration = TimeSpan.Zero;
        if (element.TryGetProperty("duration", out var durationElement) && durationElement.TryGetInt32(out var durationSeconds))
        {
            duration = TimeSpan.FromSeconds(durationSeconds);
        }

        DateTimeOffset? firstBroadcast = null;
        var firstBroadcastStr = element.GetPropertyOrDefault("first_broadcast_date");
        if (DateTimeOffset.TryParse(firstBroadcastStr, out var fb))
        {
            firstBroadcast = fb;
        }

        return new Programme
        {
            Pid = pid,
            Type = type,
            Name = displayTitle,
            Episode = subtitle,
            Description = mediumSynopsis,
            Channel = channel,
            ThumbnailUrl = thumbnailUrl,
            WebUrl = type == ProgrammeType.Tv
                ? UrlBuilder.IPlayerEpisode(pid)
                : UrlBuilder.SoundsPlay(pid),
            Duration = duration,
            FirstBroadcast = firstBroadcast
        };
    }

    /// <summary>
    /// Parse a programme element from BBC ibl search API results.
    /// Supports both ibl format (id, synopses, master_brand) and programmes format (pid, short_synopsis, ownership).
    /// </summary>
    public static Programme? ParseSearchResultElement(JsonElement element, ProgrammeType type)
    {
        // ibl uses "id", programmes format uses "pid"
        var pid = element.GetPropertyOrDefault("id") ?? element.GetPropertyOrDefault("pid");
        if (string.IsNullOrEmpty(pid))
        {
            return null;
        }

        var title = element.GetPropertyOrDefault("title") ?? string.Empty;
        var subtitle = element.GetPropertyOrDefault("subtitle")
                       ?? element.GetNestedPropertyOrDefault("display_title", "subtitle")
                       ?? string.Empty;

        // ibl uses synopses.small/medium; programmes uses short_synopsis/medium_synopsis
        var shortSynopsis = element.GetNestedPropertyOrDefault("synopses", "small")
                            ?? element.GetPropertyOrDefault("short_synopsis")
                            ?? string.Empty;
        var mediumSynopsis = element.GetNestedPropertyOrDefault("synopses", "medium")
                             ?? element.GetPropertyOrDefault("medium_synopsis")
                             ?? shortSynopsis;

        // ibl uses master_brand.titles.small; programmes uses ownership.service.title
        var channel = element.GetNestedPropertyOrDefault("master_brand", "titles", "small")
                      ?? element.GetNestedPropertyOrDefault("ownership", "service", "title")
                      ?? string.Empty;

        var thumbnailUrl = string.Empty;
        // ibl format: images.standard with {recipe} placeholder
        var standardImage = element.GetNestedPropertyOrDefault("images", "standard");
        if (!string.IsNullOrEmpty(standardImage))
        {
            thumbnailUrl = standardImage.Replace("{recipe}", "480x270", StringComparison.Ordinal);
        }
        // programmes format: image.pid
        else if (element.TryGetProperty("image", out var image))
        {
            var imagePid = image.GetPropertyOrDefault("pid");
            if (!string.IsNullOrEmpty(imagePid))
            {
                thumbnailUrl = $"https://ichef.bbci.co.uk/images/ic/480x270/{imagePid}.jpg";
            }
        }

        var webUrl = string.Empty;
        if (PidValidator.IsValid(pid))
        {
            webUrl = type == ProgrammeType.Tv
                ? UrlBuilder.IPlayerEpisode(pid)
                : UrlBuilder.SoundsPlay(pid);
        }

        return new Programme
        {
            Pid = pid,
            Type = type,
            Name = title,
            Episode = subtitle,
            Description = mediumSynopsis,
            Channel = channel,
            ThumbnailUrl = thumbnailUrl,
            WebUrl = webUrl,
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No 'programme' element in JSON response")]
    private static partial void LogNoProgrammeElement(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse JSON: {ErrorMessage}")]
    private static partial void LogJsonParseError(ILogger logger, string errorMessage);
}

/// <summary>
/// Extension methods for JsonElement for safer property access.
/// </summary>
internal static class JsonElementExtensions
{
    public static string? GetPropertyOrDefault(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    public static string? GetNestedPropertyOrDefault(this JsonElement element, params string[] path)
    {
        var current = element;
        for (var i = 0; i < path.Length - 1; i++)
        {
            if (!current.TryGetProperty(path[i], out current))
            {
                return null;
            }
        }

        return current.GetPropertyOrDefault(path[^1]);
    }
}
