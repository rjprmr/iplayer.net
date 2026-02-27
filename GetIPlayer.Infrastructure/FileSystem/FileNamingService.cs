using System.Text.RegularExpressions;

namespace GetIPlayer.Infrastructure.FileSystem;

/// <summary>
/// Service for generating safe output filenames from programme metadata.
/// OWASP A03: Sanitises filenames to prevent injection through file names.
/// </summary>
public static partial class FileNamingService
{
    /// <summary>
    /// Characters that are invalid in filenames across all platforms.
    /// </summary>
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// Generate a sanitised filename from a format template and programme data.
    /// </summary>
    /// <param name="template">Format template (e.g. "{name} - {episode} {pid}").</param>
    /// <param name="substitutions">Dictionary of placeholder values.</param>
    /// <param name="maxLength">Maximum filename length (excluding extension).</param>
    /// <returns>Sanitised filename.</returns>
    public static string GenerateFileName(
        string template,
        IReadOnlyDictionary<string, string> substitutions,
        int maxLength = 200)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        var result = template;
        foreach (var (key, value) in substitutions)
        {
            result = result.Replace($"{{{key}}}", SanitiseComponent(value), StringComparison.OrdinalIgnoreCase);
        }

        // Remove any remaining unresolved placeholders
        result = PlaceholderRegex().Replace(result, string.Empty);

        // Collapse multiple spaces/dashes
        result = MultipleSpacesRegex().Replace(result.Trim(), " ");
        result = MultipleDashesRegex().Replace(result, " - ");

        // Truncate if necessary
        if (result.Length > maxLength)
        {
            result = result[..maxLength].TrimEnd();
        }

        return result;
    }

    /// <summary>
    /// Sanitise a single component value for use in filenames.
    /// </summary>
    public static string SanitiseComponent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Replace invalid filename characters with underscore
        var result = value;
        foreach (var c in InvalidChars)
        {
            result = result.Replace(c, '_');
        }

        // Also replace characters that are problematic on some OS
        result = result.Replace(':', '-').Replace('"', '\'');

        return result.Trim();
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"\s*-(\s*-)+\s*")]
    private static partial Regex MultipleDashesRegex();
}
