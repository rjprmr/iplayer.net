using System.Threading.Channels;
using GetIPlayer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GetIPlayer.Web.Pages;

public sealed class DownloadModel : PageModel
{
    private readonly DownloadTracker _tracker;
    private readonly Channel<DownloadRequest> _downloadChannel;

    public DownloadModel(DownloadTracker tracker, Channel<DownloadRequest> downloadChannel)
    {
        _tracker = tracker;
        _downloadChannel = downloadChannel;
    }

    [BindProperty]
    public string? Target { get; set; }

    [BindProperty]
    public bool Force { get; set; }

    [BindProperty]
    public bool Subtitles { get; set; }

    [BindProperty]
    public bool Overwrite { get; set; }

    public IReadOnlyList<DownloadTracker.DownloadProgressInfo> ActiveDownloads { get; set; } = [];
    public IReadOnlyList<DownloadTracker.DownloadResultInfo> RecentCompleted { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        LoadTrackerData();
    }

    private const int MaxTargetLength = 500;

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Target))
        {
            StatusMessage = "Please enter a PID, URL, or index.";
            LoadTrackerData();
            return Page();
        }

        var trimmedTarget = Target.Trim();
        if (trimmedTarget.Length > MaxTargetLength)
        {
            StatusMessage = "Input too long.";
            LoadTrackerData();
            return Page();
        }

        var request = new DownloadRequest
        {
            Id = Guid.NewGuid().ToString("N"),
            Target = trimmedTarget,
            Force = Force,
            Subtitles = Subtitles,
            Overwrite = Overwrite
        };
        await _downloadChannel.Writer.WriteAsync(request, cancellationToken);

        StatusMessage = $"Download queued: {Target}";
        return RedirectToPage();
    }

    private void LoadTrackerData()
    {
        ActiveDownloads = _tracker.GetActiveDownloads();
        RecentCompleted = _tracker.GetRecentCompleted();
    }
}
