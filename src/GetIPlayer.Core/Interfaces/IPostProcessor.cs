using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Post-processes downloaded media (remux, tag, convert subtitles).
/// </summary>
public interface IPostProcessor
{
    /// <summary>
    /// Remux a raw transport stream into an MP4 container using ffmpeg.
    /// </summary>
    /// <param name="inputPath">Path to the raw TS file.</param>
    /// <param name="outputPath">Path for the output MP4/M4A file.</param>
    /// <param name="subtitlePath">Optional path to SRT subtitle file to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemuxAsync(
        string inputPath,
        string outputPath,
        string? subtitlePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge separate audio and video files into a single MP4.
    /// </summary>
    /// <param name="videoPath">Path to the video file.</param>
    /// <param name="audioPath">Path to the audio file.</param>
    /// <param name="outputPath">Path for the merged output file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MergeAudioVideoAsync(
        string videoPath,
        string audioPath,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tag a media file with metadata.
    /// </summary>
    /// <param name="filePath">Path to the media file.</param>
    /// <param name="metadata">Metadata to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TagAsync(
        string filePath,
        TagMetadata metadata,
        CancellationToken cancellationToken = default);
}
