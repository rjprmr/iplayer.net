using System.Threading.Channels;
using GetIPlayer.Application;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GetIPlayer.Web.Pages;

public sealed class ProgrammeModel : PageModel
{
    private readonly SearchOrchestrator _search;
    private readonly Channel<DownloadRequest> _downloadChannel;

    public ProgrammeModel(SearchOrchestrator search, Channel<DownloadRequest> downloadChannel)
    {
        _search = search;
        _downloadChannel = downloadChannel;
    }

    [BindProperty(SupportsGet = true)]
    public string Pid { get; set; } = string.Empty;

    public Programme? Programme { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Pid))
        {
            return RedirectToPage("/Search");
        }

        Programme = await _search.GetInfoAsync(Pid, cancellationToken);
        if (Programme is null)
        {
            ErrorMessage = $"Programme with PID '{Pid}' was not found.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDownloadAsync(
        string pid, bool subtitles, CancellationToken cancellationToken)
    {
        var request = new DownloadRequest
        {
            Id = Guid.NewGuid().ToString("N"),
            Target = pid,
            Subtitles = subtitles
        };
        await _downloadChannel.Writer.WriteAsync(request, cancellationToken);

        StatusMessage = "Download queued successfully.";
        return RedirectToPage("/Download");
    }
}
