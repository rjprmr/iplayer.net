using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GetIPlayer.Web.Pages.History;

public sealed class IndexModel : PageModel
{
    private readonly IHistoryService _history;

    public IndexModel(IHistoryService history) => _history = history;

    public IReadOnlyList<HistoryRecord> Records { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var all = await _history.GetAllAsync(cancellationToken);
        Records = all.OrderByDescending(r => r.DownloadedAt).ToList();
    }

    public async Task<IActionResult> OnPostClearAsync(CancellationToken cancellationToken)
    {
        await _history.ClearAsync(cancellationToken);
        StatusMessage = "Download history cleared.";
        return RedirectToPage();
    }
}
