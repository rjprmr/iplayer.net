using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GetIPlayer.Infrastructure.Streaming;

/// <summary>
/// Parses MPEG-DASH MPD manifests.
/// </summary>
public static partial class MpdParser
{
    /// <summary>
    /// Represents an adaptation set containing representations (quality levels).
    /// </summary>
    public sealed record AdaptationSet(
        string MimeType,
        string? Lang,
        IReadOnlyList<Representation> Representations);

    /// <summary>
    /// Represents a single quality/bitrate level within an adaptation set.
    /// </summary>
    public sealed record Representation(
        string Id,
        int Bandwidth,
        int? Width,
        int? Height,
        string? Codecs,
        Uri? BaseUrl,
        IReadOnlyList<Uri> SegmentUrls);

    /// <summary>
    /// Parse an MPD manifest and extract adaptation sets with representations.
    /// </summary>
    public static IReadOnlyList<AdaptationSet> ParseMpd(string xml, Uri baseUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        ArgumentNullException.ThrowIfNull(baseUri);

        var adaptationSets = new List<AdaptationSet>();

        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var periods = doc.Descendants(ns + "Period");
            foreach (var period in periods)
            {
                foreach (var adaptationSet in period.Elements(ns + "AdaptationSet"))
                {
                    var mimeType = adaptationSet.Attribute("mimeType")?.Value ?? "video/mp4";
                    var lang = adaptationSet.Attribute("lang")?.Value;

                    var representations = new List<Representation>();
                    foreach (var rep in adaptationSet.Elements(ns + "Representation"))
                    {
                        var id = rep.Attribute("id")?.Value ?? string.Empty;
                        var bandwidth = int.TryParse(rep.Attribute("bandwidth")?.Value, out var bw) ? bw : 0;
                        var width = int.TryParse(rep.Attribute("width")?.Value, out var w) ? w : (int?)null;
                        var height = int.TryParse(rep.Attribute("height")?.Value, out var h) ? h : (int?)null;
                        var codecs = rep.Attribute("codecs")?.Value;

                        Uri? repBaseUrl = null;
                        var baseUrlElement = rep.Element(ns + "BaseURL");
                        if (baseUrlElement is not null)
                        {
                            repBaseUrl = ResolveUri(baseUri, baseUrlElement.Value.Trim());
                        }

                        var segmentUrls = ParseSegmentUrls(rep, ns, baseUri);

                        representations.Add(new Representation(id, bandwidth, width, height, codecs, repBaseUrl, segmentUrls));
                    }

                    adaptationSets.Add(new AdaptationSet(mimeType, lang, representations.AsReadOnly()));
                }
            }
        }
        catch (System.Xml.XmlException)
        {
            // Return empty on parse failure
        }

        return adaptationSets.AsReadOnly();
    }

    /// <summary>
    /// Extract the total duration from the MPD manifest.
    /// </summary>
    public static TimeSpan? ParseDuration(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        try
        {
            var doc = XDocument.Parse(xml);
            var durationAttr = doc.Root?.Attribute("mediaPresentationDuration")?.Value;
            if (!string.IsNullOrEmpty(durationAttr))
            {
                return System.Xml.XmlConvert.ToTimeSpan(durationAttr);
            }
        }
        catch
        {
            // Ignore parse failures
        }

        return null;
    }

    private static ReadOnlyCollection<Uri> ParseSegmentUrls(XElement representation, XNamespace ns, Uri baseUri)
    {
        var urls = new List<Uri>();

        // SegmentList
        var segmentList = representation.Element(ns + "SegmentList");
        if (segmentList is not null)
        {
            foreach (var segUrl in segmentList.Elements(ns + "SegmentURL"))
            {
                var media = segUrl.Attribute("media")?.Value;
                if (!string.IsNullOrEmpty(media))
                {
                    urls.Add(ResolveUri(baseUri, media));
                }
            }

            return urls.AsReadOnly();
        }

        // SegmentTemplate
        var segmentTemplate = representation.Element(ns + "SegmentTemplate")
            ?? representation.Parent?.Element(ns + "SegmentTemplate");

        if (segmentTemplate is not null)
        {
            var timeline = segmentTemplate.Element(ns + "SegmentTimeline");
            if (timeline is not null)
            {
                var mediaTemplate = segmentTemplate.Attribute("media")?.Value;
                var initTemplate = segmentTemplate.Attribute("initialization")?.Value;
                var repId = representation.Attribute("id")?.Value ?? string.Empty;

                if (!string.IsNullOrEmpty(initTemplate))
                {
                    var initUrl = initTemplate
                        .Replace("$RepresentationID$", repId, StringComparison.Ordinal);
                    urls.Add(ResolveUri(baseUri, initUrl));
                }

                if (!string.IsNullOrEmpty(mediaTemplate))
                {
                    long time = 0;
                    foreach (var s in timeline.Elements(ns + "S"))
                    {
                        var t = long.TryParse(s.Attribute("t")?.Value, out var tVal) ? tVal : time;
                        var d = long.TryParse(s.Attribute("d")?.Value, out var dVal) ? dVal : 0L;
                        var r = int.TryParse(s.Attribute("r")?.Value, out var rVal) ? rVal : 0;

                        time = t;
                        for (var i = 0; i <= r; i++)
                        {
                            var segUrl = mediaTemplate
                                .Replace("$RepresentationID$", repId, StringComparison.Ordinal)
                                .Replace("$Time$", time.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
                            urls.Add(ResolveUri(baseUri, segUrl));
                            time += d;
                        }
                    }
                }
            }
        }

        return urls.AsReadOnly();
    }

    private static Uri ResolveUri(Uri baseUri, string relative)
    {
        if (Uri.TryCreate(relative, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return new Uri(baseUri, relative);
    }
}
