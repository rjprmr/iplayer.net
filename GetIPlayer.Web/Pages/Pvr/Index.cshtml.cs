using GetIPlayer.Application;
using GetIPlayer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GetIPlayer.Web.Pages.Pvr;

public sealed class IndexModel : PageModel
{
    private readonly PvrOrchestrator _pvr;

    public IndexModel(PvrOrchestrator pvr) => _pvr = pvr;

    public IReadOnlyList<PvrSearch> Searches { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Searches = await _pvr.ListSearchesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleAsync(
        string name, bool enabled, CancellationToken cancellationToken)
    {
        await _pvr.SetSearchEnabledAsync(name, enabled, cancellationToken);
        StatusMessage = $"PVR search '{name}' {(enabled ? "enabled" : "disabled")}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        string name, CancellationToken cancellationToken)
    {
        await _pvr.DeleteSearchAsync(name, cancellationToken);
        StatusMessage = $"PVR search '{name}' deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRunAllAsync(CancellationToken cancellationToken)
    {
        var results = await _pvr.RunAllAsync(cancellationToken: cancellationToken);
        var successful = results.Count(r => r.Status == GetIPlayer.Core.Enums.DownloadStatus.Success);
        StatusMessage = $"PVR run complete: {results.Count} processed, {successful} successful.";
        return RedirectToPage();
    }
}
