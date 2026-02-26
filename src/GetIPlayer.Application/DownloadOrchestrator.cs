using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Exceptions;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GetIPlayer.Application;

/// <summary>
/// Orchestrates the complete download workflow for a programme:
/// stream discovery → download segments → post-process → tag → history.
/// </summary>
public sealed partial class DownloadOrchestrator : IDownloadService
{
    private readonly IProgrammeService _programmeService;
    private readonly IStreamService _streamService;
    private readonly IHlsDownloader _hlsDownloader;
    private readonly IDashDownloader _dashDownloader;
    private readonly IPostProcessor _postProcessor;
    private readonly ISubtitleService _subtitleService;
    private readonly IHistoryService _historyService;
    private readonly ICacheService _cacheService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DownloadOrchestrator> _logger;
    private readonly DownloadOptions _downloadOptions;
    private readonly OutputOptions _outputOptions;

    public DownloadOrchestrator(
        IProgrammeService programmeService,
        IStreamService streamService,
        IHlsDownloader hlsDownloader,
        IDashDownloader dashDownloader,
        IPostProcessor postProcessor,
        ISubtitleService subtitleService,
        IHistoryService historyService,
        ICacheService cacheService,
        IFileSystem fileSystem,
        ILogger<DownloadOrchestrator> logger,
        IOptions<DownloadOptions> downloadOptions,
        IOptions<OutputOptions> outputOptions)
    {
        _programmeService = programmeService ?? throw new ArgumentNullException(nameof(programmeService));
        _streamService = streamService ?? throw new ArgumentNullException(nameof(streamService));
        _hlsDownloader = hlsDownloader ?? throw new ArgumentNullException(nameof(hlsDownloader));
        _dashDownloader = dashDownloader ?? throw new ArgumentNullException(nameof(dashDownloader));
        _postProcessor = postProcessor ?? throw new ArgumentNullException(nameof(postProcessor));
        _subtitleService = subtitleService ?? throw new ArgumentNullException(nameof(subtitleService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _downloadOptions = downloadOptions?.Value ?? throw new ArgumentNullException(nameof(downloadOptions));
        _outputOptions = outputOptions?.Value ?? throw new ArgumentNullException(nameof(outputOptions));
    }

    /// <inheritdoc />
    public async Task<DownloadResult> DownloadAsync(
        string pid,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pid);

        var startTime = DateTimeOffset.UtcNow;
        LogDownloadStarting(_logger, pid);

        try
        {
            // 1. Check history (skip if already downloaded and not forcing)
            if (!_downloadOptions.Force && await _historyService.ExistsAsync(pid, cancellationToken).ConfigureAwait(false))
            {
                LogAlreadyDownloaded(_logger, pid);
                return new DownloadResult
                {
                    Pid = pid,
                    Status = DownloadStatus.Skipped,
                    ErrorMessage = "Already in download history (use --force to re-download)"
                };
            }

            // 2. Get programme metadata
            progress?.Report(new DownloadProgress { Pid = pid, Phase = "Fetching metadata" });
            var programme = await _programmeService.GetByPidAsync(pid, cancellationToken).ConfigureAwait(false);
            if (programme is null)
            {
                return new DownloadResult
                {
                    Pid = pid,
                    Status = DownloadStatus.Failed,
                    ErrorMessage = $"Programme {pid} not found"
                };
            }

            // 3. Discover streams
            progress?.Report(new DownloadProgress { Pid = pid, Phase = "Discovering streams" });
            var qualityPrefs = programme.Type == ProgrammeType.Tv
                ? _downloadOptions.TvQuality
                : _downloadOptions.RadioQuality;

            var streams = await _streamService.GetStreamsAsync(
                pid, qualityPrefs, _downloadOptions.Versions, cancellationToken).ConfigureAwait(false);

            if (streams.Count == 0)
            {
                return new DownloadResult
                {
                    Pid = pid,
                    Status = DownloadStatus.Unavailable,
                    ErrorMessage = "No available streams found"
                };
            }

            var selectedStream = streams[0]; // Best available
            LogStreamSelected(_logger, pid, selectedStream.Mode, selectedStream.Quality);

            // 4. Determine output path
            var outputPath = BuildOutputPath(programme, selectedStream);
            var rawPath = outputPath + ".raw";

            // Check for existing file (skip if not overwriting)
            if (!_downloadOptions.Overwrite && _fileSystem.FileExists(outputPath))
            {
                LogFileExists(_logger, outputPath);
                return new DownloadResult
                {
                    Pid = pid,
                    Status = DownloadStatus.Skipped,
                    FilePath = outputPath,
                    ErrorMessage = "File already exists (use --overwrite to replace)"
                };
            }

            // 5. Download segments
            progress?.Report(new DownloadProgress { Pid = pid, Phase = "Downloading" });
            long bytesDownloaded;

            if (selectedStream.Protocol == StreamProtocol.Hls)
            {
                bytesDownloaded = await _hlsDownloader.DownloadAsync(
                    selectedStream, rawPath, progress, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bytesDownloaded = await _dashDownloader.DownloadAsync(
                    selectedStream, rawPath, progress, cancellationToken).ConfigureAwait(false);
            }

            // 6. Download subtitles (if enabled)
            string? subtitlePath = null;
            if (_downloadOptions.Subtitles)
            {
                progress?.Report(new DownloadProgress { Pid = pid, Phase = "Downloading subtitles" });
                var srtPath = Path.ChangeExtension(outputPath, ".srt");
                var subs = await _subtitleService.DownloadAsync(pid, srtPath, cancellationToken).ConfigureAwait(false);
                if (subs is not null)
                {
                    subtitlePath = srtPath;
                }
            }

            // 7. Remux to MP4/M4A
            progress?.Report(new DownloadProgress { Pid = pid, Phase = "Remuxing" });

            // Check if DASH produced separate audio/video
            var videoTmp = rawPath + ".video.tmp";
            var audioTmp = rawPath + ".audio.tmp";

            if (_fileSystem.FileExists(videoTmp) && _fileSystem.FileExists(audioTmp))
            {
                await _postProcessor.MergeAudioVideoAsync(
                    videoTmp, audioTmp, outputPath, cancellationToken).ConfigureAwait(false);
            }
            else if (_fileSystem.FileExists(rawPath))
            {
                await _postProcessor.RemuxAsync(
                    rawPath, outputPath, subtitlePath, cancellationToken).ConfigureAwait(false);
            }

            // 8. Tag metadata
            if (_downloadOptions.Tag)
            {
                progress?.Report(new DownloadProgress { Pid = pid, Phase = "Tagging" });
                var tagMetadata = BuildTagMetadata(programme);
                await _postProcessor.TagAsync(outputPath, tagMetadata, cancellationToken).ConfigureAwait(false);
            }

            // 9. Record in history
            var duration = DateTimeOffset.UtcNow - startTime;
            var fileSize = _fileSystem.FileExists(outputPath) ? _fileSystem.GetFileSize(outputPath) : 0;

            var historyRecord = new HistoryRecord
            {
                Pid = pid,
                Name = programme.Name,
                Episode = programme.Episode,
                Type = programme.Type,
                DownloadedAt = DateTimeOffset.UtcNow,
                Mode = selectedStream.Mode,
                FilePath = outputPath,
                FileSize = fileSize,
                Channel = programme.Channel,
                Version = selectedStream.Mode,
                Duration = programme.Duration,
                ThumbnailUrl = programme.ThumbnailUrl
            };
            await _historyService.AddAsync(historyRecord, cancellationToken).ConfigureAwait(false);

            LogDownloadComplete(_logger, pid, outputPath, bytesDownloaded);

            return new DownloadResult
            {
                Pid = pid,
                Status = DownloadStatus.Success,
                FilePath = outputPath,
                Mode = selectedStream.Mode,
                BytesDownloaded = bytesDownloaded,
                Duration = duration
            };
        }
        catch (GeoBlockedException)
        {
            return new DownloadResult
            {
                Pid = pid,
                Status = DownloadStatus.Failed,
                ErrorMessage = "Content is geo-blocked. Use a UK VPN or proxy."
            };
        }
        catch (StreamUnavailableException ex)
        {
            return new DownloadResult
            {
                Pid = pid,
                Status = DownloadStatus.Unavailable,
                ErrorMessage = ex.Message
            };
        }
        catch (OperationCanceledException)
        {
            return new DownloadResult
            {
                Pid = pid,
                Status = DownloadStatus.Cancelled,
                ErrorMessage = "Download was cancelled"
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogDownloadFailed(_logger, pid, ex.Message);
            return new DownloadResult
            {
                Pid = pid,
                Status = DownloadStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<DownloadResult> DownloadByIndexAsync(
        int index,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Try TV first, then radio
        var programme = await _cacheService.GetByIndexAsync(index, ProgrammeType.Tv, cancellationToken).ConfigureAwait(false)
            ?? await _cacheService.GetByIndexAsync(index, ProgrammeType.Radio, cancellationToken).ConfigureAwait(false);

        if (programme is null)
        {
            return new DownloadResult
            {
                Pid = $"index:{index}",
                Status = DownloadStatus.Failed,
                ErrorMessage = $"No programme found at index {index}. Try refreshing the cache."
            };
        }

        return await DownloadAsync(programme.Pid, progress, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<DownloadResult> DownloadByUrlAsync(
        string url,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var pid = Infrastructure.Bbc.PidValidator.ExtractFromUrl(url);
        if (string.IsNullOrEmpty(pid))
        {
            return Task.FromResult(new DownloadResult
            {
                Pid = url,
                Status = DownloadStatus.Failed,
                ErrorMessage = $"Could not extract PID from URL: {url}"
            });
        }

        return DownloadAsync(pid, progress, cancellationToken);
    }

    private string BuildOutputPath(Programme programme, StreamInfo stream)
    {
        var dir = _outputOptions.OutputDir ?? Environment.CurrentDirectory;
        var extension = programme.Type == ProgrammeType.Tv ? ".mp4" : ".m4a";

        // Build filename from template
        var filename = _outputOptions.FilePrefix
            .Replace("<name>", programme.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("<episode>", programme.Episode, StringComparison.OrdinalIgnoreCase)
            .Replace("<pid>", programme.Pid, StringComparison.OrdinalIgnoreCase)
            .Replace("<channel>", programme.Channel, StringComparison.OrdinalIgnoreCase)
            .Replace("<mode>", stream.Mode, StringComparison.OrdinalIgnoreCase);

        if (_outputOptions.AddVersionToFilename)
        {
            filename += $"_{stream.Mode}";
        }

        // Sanitize filename
        filename = Infrastructure.FileSystem.FileNamingService.SanitiseComponent(filename);

        if (_outputOptions.MaxFilenameLength > 0 && filename.Length > _outputOptions.MaxFilenameLength)
        {
            filename = filename[.._outputOptions.MaxFilenameLength];
        }

        return Path.Combine(dir, filename + extension);
    }

    private static TagMetadata BuildTagMetadata(Programme programme) => new()
    {
        Title = $"{programme.Name} - {programme.Episode}".TrimEnd(' ', '-'),
        Artist = programme.Channel,
        Album = programme.Name,
        Genre = programme.Type == ProgrammeType.Tv ? "TV" : "Podcast",
        Description = programme.Description,
        LongDescription = programme.Description,
        Year = programme.FirstBroadcast?.Year ?? 0,
        Network = "BBC",
        Copyright = $"BBC {programme.FirstBroadcast?.Year ?? DateTime.UtcNow.Year}",
        Comment = $"Downloaded from BBC iPlayer/Sounds",
        IsPodcast = programme.Type == ProgrammeType.Radio,
    };

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting download for {Pid}")]
    private static partial void LogDownloadStarting(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Information, Message = "Programme {Pid} already downloaded, skipping")]
    private static partial void LogAlreadyDownloaded(ILogger logger, string pid);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Selected stream for {Pid}: mode={Mode} quality={Quality}")]
    private static partial void LogStreamSelected(ILogger logger, string pid, string mode, QualityLevel quality);

    [LoggerMessage(Level = LogLevel.Information, Message = "File already exists: {FilePath}")]
    private static partial void LogFileExists(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Download complete for {Pid}: {FilePath} ({Bytes} bytes)")]
    private static partial void LogDownloadComplete(ILogger logger, string pid, string filePath, long bytes);

    [LoggerMessage(Level = LogLevel.Error, Message = "Download failed for {Pid}: {ErrorMessage}")]
    private static partial void LogDownloadFailed(ILogger logger, string pid, string errorMessage);
}
