using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Interfaces;

/// <summary>
/// Downloads and converts subtitles.
/// </summary>
public interface ISubtitleService
{
    /// <summary>
    /// Download subtitles for a programme and convert to SRT.
    /// </summary>
    /// <param name="pid">BBC programme identifier.</param>
    /// <param name="outputPath">Path to save the SRT file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Subtitle data, or null if no subtitles are available.</returns>
    Task<SubtitleData?> DownloadAsync(
        string pid,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert TTML subtitle content to SRT format.
    /// </summary>
    /// <param name="ttmlContent">TTML XML content.</param>
    /// <returns>SRT formatted string.</returns>
    string ConvertTtmlToSrt(string ttmlContent);
}
