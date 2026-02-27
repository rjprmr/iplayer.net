using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Infrastructure.Bbc;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Infrastructure.PostProcessing;

/// <summary>
/// Downloads BBC subtitles (TTML) and converts to SRT format.
/// </summary>
public sealed partial class SubtitleConverter : ISubtitleService
{
    private readonly IHttpClientService _httpClient;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SubtitleConverter> _logger;

    public SubtitleConverter(
        IHttpClientService httpClient,
        IFileSystem fileSystem,
        ILogger<SubtitleConverter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SubtitleData?> DownloadAsync(
        string pid,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        PidValidator.EnsureValid(pid);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        // BBC subtitle URL pattern
        var subtitleUrl = $"https://www.bbc.co.uk/programmes/{Uri.EscapeDataString(pid)}/subtitles";
        LogDownloadingSubtitles(_logger, pid, subtitleUrl);

        string ttmlContent;
        try
        {
            ttmlContent = await _httpClient.GetStringAsync(subtitleUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            LogNoSubtitles(_logger, pid);
            return null;
        }

        if (string.IsNullOrWhiteSpace(ttmlContent) || !ttmlContent.Contains("<tt", StringComparison.OrdinalIgnoreCase))
        {
            LogNoSubtitles(_logger, pid);
            return null;
        }

        var srtContent = ConvertTtmlToSrt(ttmlContent);

        // Write SRT file
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            _fileSystem.CreateDirectory(dir);
        }

        await _fileSystem.WriteAllTextAsync(outputPath, srtContent, cancellationToken).ConfigureAwait(false);

        LogSubtitlesSaved(_logger, pid, outputPath);

        return new SubtitleData
        {
            Pid = pid,
            SrtContent = srtContent,
            TtmlContent = ttmlContent,
            Language = "en"
        };
    }

    /// <inheritdoc />
    public string ConvertTtmlToSrt(string ttmlContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ttmlContent);

        var sb = new StringBuilder();
        var index = 1;

        try
        {
            var doc = XDocument.Parse(ttmlContent);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            XNamespace ttNs = "http://www.w3.org/ns/ttml";

            // Try both namespaced and non-namespaced
            var body = doc.Descendants(ns + "body").FirstOrDefault()
                ?? doc.Descendants(ttNs + "body").FirstOrDefault()
                ?? doc.Descendants("body").FirstOrDefault();

            if (body is null)
            {
                return string.Empty;
            }

            var paragraphs = body.Descendants(ns + "p")
                .Concat(body.Descendants(ttNs + "p"))
                .Concat(body.Descendants("p"))
                .DistinctBy(p => p.Attribute("begin")?.Value + p.Attribute("end")?.Value);

            foreach (var p in paragraphs)
            {
                var beginStr = p.Attribute("begin")?.Value;
                var endStr = p.Attribute("end")?.Value;

                if (string.IsNullOrEmpty(beginStr) || string.IsNullOrEmpty(endStr))
                {
                    continue;
                }

                var begin = ParseTimecode(beginStr);
                var end = ParseTimecode(endStr);

                if (begin is null || end is null)
                {
                    continue;
                }

                var text = ExtractText(p).Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                sb.AppendLine(index.ToString(CultureInfo.InvariantCulture));
                sb.Append(FormatSrtTime(begin.Value));
                sb.Append(" --> ");
                sb.AppendLine(FormatSrtTime(end.Value));
                sb.AppendLine(text);
                sb.AppendLine(); // Blank line separator
                index++;
            }
        }
        catch (System.Xml.XmlException ex)
        {
            LogTtmlParseError(_logger, ex.Message);
        }

        return sb.ToString();
    }

    private static TimeSpan? ParseTimecode(string timecode)
    {
        // Handle HH:MM:SS.mmm or HH:MM:SS:FF formats
        if (TimeSpan.TryParse(timecode, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        // BBC sometimes uses HH:MM:SS:FF (frames) format
        var match = TimecodeRegex().Match(timecode);
        if (match.Success)
        {
            var hours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var seconds = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var frames = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            var ms = (int)(frames / 25.0 * 1000); // Assume 25fps PAL

            return new TimeSpan(0, hours, minutes, seconds, ms);
        }

        return null;
    }

    private static string FormatSrtTime(TimeSpan ts) =>
        $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";

    private static string ExtractText(XElement element)
    {
        var sb = new StringBuilder();

        foreach (var node in element.Nodes())
        {
            if (node is XText text)
            {
                sb.Append(text.Value);
            }
            else if (node is XElement child)
            {
                if (child.Name.LocalName == "br")
                {
                    sb.Append('\n');
                }
                else if (child.Name.LocalName == "span")
                {
                    sb.Append(ExtractText(child));
                }
            }
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"^(\d{2}):(\d{2}):(\d{2}):(\d{2})$")]
    private static partial Regex TimecodeRegex();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Downloading subtitles for {Pid} from {Url}")]
    private static partial void LogDownloadingSubtitles(ILogger logger, string pid, string url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No subtitles available for {Pid}")]
    private static partial void LogNoSubtitles(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subtitles saved for {Pid}: {OutputPath}")]
    private static partial void LogSubtitlesSaved(ILogger logger, string pid, string outputPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse TTML subtitles: {ErrorMessage}")]
    private static partial void LogTtmlParseError(ILogger logger, string errorMessage);
}
