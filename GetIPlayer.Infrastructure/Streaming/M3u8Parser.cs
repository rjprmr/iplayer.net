using System.Text.RegularExpressions;

namespace GetIPlayer.Infrastructure.Streaming;

/// <summary>
/// Parses M3U8 playlists for HLS streaming.
/// </summary>
public static partial class M3u8Parser
{
    /// <summary>
    /// Represents a parsed HLS variant from a master playlist.
    /// </summary>
    public sealed record HlsVariant(
        Uri Url,
        int Bandwidth,
        int? Width,
        int? Height,
        string? Codecs);

    /// <summary>
    /// Represents a media segment from a media playlist.
    /// </summary>
    public sealed record Segment(
        Uri Url,
        double Duration,
        int SequenceNumber);

    /// <summary>
    /// Parse a master playlist and extract variant streams.
    /// </summary>
    public static IReadOnlyList<HlsVariant> ParseMasterPlaylist(string content, Uri baseUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(baseUri);

        var variants = new List<HlsVariant>();
        var lines = content.Split('\n', StringSplitOptions.TrimEntries);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("#EXT-X-STREAM-INF:", StringComparison.Ordinal))
            {
                continue;
            }

            var attributes = lines[i]["#EXT-X-STREAM-INF:".Length..];
            var bandwidth = ParseIntAttribute(attributes, "BANDWIDTH");
            var resolution = ParseStringAttribute(attributes, "RESOLUTION");
            var codecs = ParseStringAttribute(attributes, "CODECS");

            int? width = null;
            int? height = null;
            if (!string.IsNullOrEmpty(resolution))
            {
                var parts = resolution.Split('x');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var w) &&
                    int.TryParse(parts[1], out var h))
                {
                    width = w;
                    height = h;
                }
            }

            // Next non-comment line is the URI
            for (var j = i + 1; j < lines.Length; j++)
            {
                var line = lines[j];
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var segmentUri = ResolveUri(baseUri, line);
                variants.Add(new HlsVariant(segmentUri, bandwidth, width, height, codecs));
                break;
            }
        }

        return variants.AsReadOnly();
    }

    /// <summary>
    /// Parse a media playlist and extract segments.
    /// </summary>
    public static IReadOnlyList<Segment> ParseMediaPlaylist(string content, Uri baseUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(baseUri);

        var segments = new List<Segment>();
        var lines = content.Split('\n', StringSplitOptions.TrimEntries);
        var sequence = 0;
        var currentDuration = 0.0;

        // Parse media sequence
        foreach (var line in lines)
        {
            if (line.StartsWith("#EXT-X-MEDIA-SEQUENCE:", StringComparison.Ordinal))
            {
                if (int.TryParse(line["#EXT-X-MEDIA-SEQUENCE:".Length..], out var seq))
                {
                    sequence = seq;
                }

                break;
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("#EXTINF:", StringComparison.Ordinal))
            {
                var durationStr = lines[i]["#EXTINF:".Length..].TrimEnd(',');
                // Remove trailing comma from EXTINF value
                if (durationStr.Contains(',', StringComparison.Ordinal))
                {
                    durationStr = durationStr[..durationStr.IndexOf(',', StringComparison.Ordinal)];
                }

                if (double.TryParse(durationStr, System.Globalization.CultureInfo.InvariantCulture, out var duration))
                {
                    currentDuration = duration;
                }

                // Next non-comment line is the segment URI
                for (var j = i + 1; j < lines.Length; j++)
                {
                    var line = lines[j];
                    if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    {
                        continue;
                    }

                    var segmentUri = ResolveUri(baseUri, line);
                    segments.Add(new Segment(segmentUri, currentDuration, sequence));
                    sequence++;
                    break;
                }
            }
        }

        return segments.AsReadOnly();
    }

    /// <summary>
    /// Check if the content is a master playlist.
    /// </summary>
    public static bool IsMasterPlaylist(string content) =>
        content.Contains("#EXT-X-STREAM-INF:", StringComparison.Ordinal);

    private static int ParseIntAttribute(string attributes, string name)
    {
        var match = AttributeRegex().Match(attributes.Replace("BANDWIDTH", name));
        // Search for the named attribute
        var pattern = $"{name}=";
        var idx = attributes.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return 0;

        var start = idx + pattern.Length;
        var end = attributes.IndexOf(',', start);
        if (end < 0) end = attributes.Length;

        var value = attributes[start..end].Trim();
        return int.TryParse(value, out var result) ? result : 0;
    }

    private static string? ParseStringAttribute(string attributes, string name)
    {
        var pattern = $"{name}=";
        var idx = attributes.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var start = idx + pattern.Length;
        if (start < attributes.Length && attributes[start] == '"')
        {
            start++;
            var end = attributes.IndexOf('"', start);
            return end < 0 ? null : attributes[start..end];
        }
        else
        {
            var end = attributes.IndexOf(',', start);
            if (end < 0) end = attributes.Length;
            return attributes[start..end].Trim();
        }
    }

    private static Uri ResolveUri(Uri baseUri, string relative)
    {
        if (Uri.TryCreate(relative, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return new Uri(baseUri, relative);
    }

    [GeneratedRegex(@"(\w+)=([^,]+)")]
    private static partial Regex AttributeRegex();
}
