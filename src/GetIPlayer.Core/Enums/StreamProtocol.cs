namespace GetIPlayer.Core.Enums;

/// <summary>
/// Streaming protocol used for content delivery.
/// </summary>
public enum StreamProtocol
{
    /// <summary>HTTP Live Streaming (Apple HLS / M3U8).</summary>
    Hls,

    /// <summary>MPEG Dynamic Adaptive Streaming over HTTP.</summary>
    Dash
}
