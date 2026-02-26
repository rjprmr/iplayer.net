namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// HTTP client abstraction for making web requests with retry and proxy support.
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// Send a GET request with retry logic.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response body as string.</returns>
    Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a GET request and return the raw bytes.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response body as byte array.</returns>
    Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a GET request and stream the response to a file.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="outputPath">File path to write the response to.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total bytes downloaded.</returns>
    Task<long> DownloadToFileAsync(
        string url,
        string outputPath,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a GET request and return the response stream.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response stream.</returns>
    Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default);
}
