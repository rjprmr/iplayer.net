using GetIPlayer.Core.Configuration;
using GetIPlayer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace GetIPlayer.Web.Pages;

public sealed class SettingsModel : PageModel
{
    private readonly OptionsFileService _optionsService;
    private readonly IOptions<DownloadOptions> _downloadOptions;
    private readonly IOptions<OutputOptions> _outputOptions;
    private readonly IOptions<ProxySettings> _proxySettings;

    public SettingsModel(
        OptionsFileService optionsService,
        IOptions<DownloadOptions> downloadOptions,
        IOptions<OutputOptions> outputOptions,
        IOptions<ProxySettings> proxySettings)
    {
        _optionsService = optionsService;
        _downloadOptions = downloadOptions;
        _outputOptions = outputOptions;
        _proxySettings = proxySettings;
    }

    // Download options
    [BindProperty]
    public bool DownloadSubtitles { get; set; }

    [BindProperty]
    public bool DownloadThumbnail { get; set; }

    [BindProperty]
    public bool TagFiles { get; set; }

    [BindProperty]
    public bool OverwriteFiles { get; set; }

    [BindProperty]
    public bool ForceDownload { get; set; }

    [BindProperty]
    public int MaxConcurrentSegments { get; set; }

    [BindProperty]
    public string FfmpegPath { get; set; } = "ffmpeg";

    [BindProperty]
    public string AtomicParsleyPath { get; set; } = "AtomicParsley";

    // Output options
    [BindProperty]
    public string OutputDir { get; set; } = string.Empty;

    [BindProperty]
    public string FilePrefix { get; set; } = string.Empty;

    [BindProperty]
    public string SubDir { get; set; } = string.Empty;

    [BindProperty]
    public bool AddVersionToFilename { get; set; }

    [BindProperty]
    public int MaxFilenameLength { get; set; }

    // Proxy
    [BindProperty]
    public string? ProxyUrl { get; set; }

    [BindProperty]
    public bool ProxyDisabled { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        LoadCurrentSettings();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var settings = await _optionsService.LoadAsync(cancellationToken);

        // Apply download options
        settings.Download.Subtitles = DownloadSubtitles;
        settings.Download.Thumbnail = DownloadThumbnail;
        settings.Download.Tag = TagFiles;
        settings.Download.Overwrite = OverwriteFiles;
        settings.Download.Force = ForceDownload;
        settings.Download.MaxConcurrentSegments = MaxConcurrentSegments > 0 ? MaxConcurrentSegments : 4;
        settings.Download.FfmpegPath = string.IsNullOrWhiteSpace(FfmpegPath) ? "ffmpeg" : FfmpegPath.Trim();
        settings.Download.AtomicParsleyPath = string.IsNullOrWhiteSpace(AtomicParsleyPath) ? "AtomicParsley" : AtomicParsleyPath.Trim();

        // Apply output options
        settings.Output.OutputDir = OutputDir?.Trim() ?? string.Empty;
        settings.Output.FilePrefix = string.IsNullOrWhiteSpace(FilePrefix) ? "{name} - {episode} {pid}" : FilePrefix.Trim();
        settings.Output.SubDir = SubDir?.Trim() ?? string.Empty;
        settings.Output.AddVersionToFilename = AddVersionToFilename;
        settings.Output.MaxFilenameLength = MaxFilenameLength > 0 ? MaxFilenameLength : 200;

        // Apply proxy
        settings.Proxy.Url = string.IsNullOrWhiteSpace(ProxyUrl) ? string.Empty : ProxyUrl.Trim();
        settings.Proxy.Disabled = ProxyDisabled;

        await _optionsService.SaveAsync(settings, cancellationToken);

        StatusMessage = "Settings saved. Restart the application for changes to take full effect.";
        return RedirectToPage();
    }

    private void LoadCurrentSettings()
    {
        var dl = _downloadOptions.Value;
        DownloadSubtitles = dl.Subtitles;
        DownloadThumbnail = dl.Thumbnail;
        TagFiles = dl.Tag;
        OverwriteFiles = dl.Overwrite;
        ForceDownload = dl.Force;
        MaxConcurrentSegments = dl.MaxConcurrentSegments;
        FfmpegPath = dl.FfmpegPath;
        AtomicParsleyPath = dl.AtomicParsleyPath;

        var output = _outputOptions.Value;
        OutputDir = output.OutputDir;
        FilePrefix = output.FilePrefix;
        SubDir = output.SubDir;
        AddVersionToFilename = output.AddVersionToFilename;
        MaxFilenameLength = output.MaxFilenameLength;

        var proxy = _proxySettings.Value;
        ProxyUrl = proxy.Url;
        ProxyDisabled = proxy.Disabled;
    }
}
