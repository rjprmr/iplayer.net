using System.Text.RegularExpressions;

namespace GetIPlayer.Infrastructure.Bbc;

/// <summary>
/// Validates BBC Programme Identifiers (PIDs).
/// OWASP A03: Input validation at the boundary.
/// </summary>
public static partial class PidValidator
{
    /// <summary>
    /// PID format: 8 or more alphanumeric characters (excluding vowels from first char set).
    /// Matches the original Perl regex: /^[b-df-hj-np-tv-z0-9]{8,}$/
    /// </summary>
    [GeneratedRegex(@"^[b-df-hj-np-tv-z0-9]{8,}$", RegexOptions.Compiled)]
    private static partial Regex PidPattern();

    /// <summary>
    /// Validate that a string is a valid BBC PID.
    /// </summary>
    /// <param name="pid">The value to validate.</param>
    /// <returns>True if the PID format is valid.</returns>
    public static bool IsValid(string? pid)
    {
        return !string.IsNullOrWhiteSpace(pid) && PidPattern().IsMatch(pid);
    }

    /// <summary>
    /// Validate a PID and throw if invalid.
    /// </summary>
    /// <param name="pid">The PID to validate.</param>
    /// <exception cref="Core.Exceptions.ValidationException">If the PID is invalid.</exception>
    public static void EnsureValid(string? pid)
    {
        if (!IsValid(pid))
        {
            throw new Core.Exceptions.ValidationException("pid", $"Invalid BBC PID format: '{pid}'");
        }
    }

    /// <summary>
    /// Extract a PID from a BBC URL.
    /// </summary>
    /// <param name="url">A BBC iPlayer or Sounds URL.</param>
    /// <returns>The extracted PID, or null if not found.</returns>
    public static string? ExtractFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var match = UrlPidPattern().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"(?:bbc\.co\.uk|bbc\.com)/(?:iplayer/episode|sounds/play)/([b-df-hj-np-tv-z0-9]{8,})", RegexOptions.Compiled)]
    private static partial Regex UrlPidPattern();
}
