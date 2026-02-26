using GetIPlayer.Application;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GetIPlayer.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IHistoryService _history;
    private readonly PvrOrchestrator _pvr;
    private readonly DownloadTracker _tracker;

    public IndexModel(IHistoryService history, PvrOrchestrator pvr, DownloadTracker tracker)
    {
        _history = history;
        _pvr = pvr;
        _tracker = tracker;
    }

    public int ActiveDownloadCount { get; set; }
    public int RecentCompletedCount { get; set; }
    public int HistoryCount { get; set; }
    public int PvrSearchCount { get; set; }
    public IReadOnlyList<HistoryRecord> RecentHistory { get; set; } = [];
    public IReadOnlyList<DownloadTracker.DownloadProgressInfo> ActiveDownloads { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ActiveDownloads = _tracker.GetActiveDownloads();
        ActiveDownloadCount = ActiveDownloads.Count;
        RecentCompletedCount = _tracker.GetRecentCompleted().Count;

        var history = await _history.GetAllAsync(cancellationToken);
        HistoryCount = history.Count;
        RecentHistory = history.OrderByDescending(r => r.DownloadedAt).Take(5).ToList();

        var searches = await _pvr.ListSearchesAsync(cancellationToken);
        PvrSearchCount = searches.Count;
    }
}
