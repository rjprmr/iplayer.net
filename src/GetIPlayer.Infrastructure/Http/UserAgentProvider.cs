namespace GetIPlayer.Infrastructure.Http;

/// <summary>
/// Provides rotating user-agent strings to avoid request fingerprinting.
/// </summary>
public sealed class UserAgentProvider
{
    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0",
    ];

    private int _index;

    /// <summary>
    /// Get the next user-agent string in the rotation.
    /// </summary>
    public string GetNext()
    {
        var index = Interlocked.Increment(ref _index) % UserAgents.Length;
        return UserAgents[index];
    }
}
