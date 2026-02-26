using System.Net;
using GetIPlayer.Core.Configuration;
using Microsoft.Extensions.Options;

namespace GetIPlayer.Infrastructure.Http;

/// <summary>
/// Configures an HttpClientHandler with proxy settings.
/// OWASP A10 (SSRF): Validates proxy URLs against allowed schemes.
/// </summary>
public sealed class ProxyHandler
{
    private static readonly HashSet<string> AllowedProxySchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "socks5"
    };

    /// <summary>
    /// Configure proxy on an HttpClientHandler based on application settings.
    /// </summary>
    public static void Configure(HttpClientHandler handler, ProxySettings settings)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Disabled)
        {
            handler.UseProxy = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Url))
        {
            return;
        }

        if (!Uri.TryCreate(settings.Url, UriKind.Absolute, out var proxyUri))
        {
            throw new ArgumentException($"Invalid proxy URL: {settings.Url}");
        }

        if (!AllowedProxySchemes.Contains(proxyUri.Scheme))
        {
            throw new ArgumentException(
                $"Proxy scheme '{proxyUri.Scheme}' is not allowed. Allowed schemes: {string.Join(", ", AllowedProxySchemes)}");
        }

        handler.UseProxy = true;
        handler.Proxy = new WebProxy(proxyUri)
        {
            BypassProxyOnLocal = true
        };
    }
}
