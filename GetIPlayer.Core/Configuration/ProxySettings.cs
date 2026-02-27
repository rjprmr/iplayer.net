namespace GetIPlayer.Core.Configuration;

/// <summary>
/// HTTP proxy configuration.
/// </summary>
public sealed class ProxySettings
{
    /// <summary>
    /// Proxy URL (e.g. "http://proxy:8080"). 
    /// Credentials should be stored separately, not in the URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>Whether to disable proxy usage entirely.</summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether to use partial proxying (proxy only for content that requires it).
    /// </summary>
    public bool PartialProxy { get; set; }
}
