using System.Threading.Channels;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GetIPlayer.Web.Pages;

public sealed class SearchModel : PageModel
{
    private readonly SearchOrchestrator _search;
    private readonly Channel<DownloadRequest> _downloadChannel;

    public SearchModel(SearchOrchestrator search, Channel<DownloadRequest> downloadChannel)
    {
        _search = search;
        _downloadChannel = downloadChannel;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ChannelFilter { get; set; }

    public SearchResult? Results { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    private const int MaxSearchLength = 200;
    private const int MaxBatchSize = 50;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            return;
        }

        // Input validation: truncate overly long search terms
        if (SearchTerm.Length > MaxSearchLength)
        {
            SearchTerm = SearchTerm[..MaxSearchLength];
        }

        var types = TypeFilter?.ToUpperInvariant() switch
        {
            "TV" => new[] { ProgrammeType.Tv },
            "RADIO" => new[] { ProgrammeType.Radio },
            _ => new[] { ProgrammeType.Tv, ProgrammeType.Radio }
        };

        Results = await _search.SearchAsync(SearchTerm, types, ChannelFilter, cancellationToken);
    }

    public async Task<IActionResult> OnPostDownloadSelectedAsync(
        [FromForm] string[] selectedPids, CancellationToken cancellationToken)
    {
        if (selectedPids is null || selectedPids.Length == 0)
        {
            StatusMessage = "No programmes selected.";
            return RedirectToPage();
        }

        // Limit batch size to prevent abuse
        var pidsToQueue = selectedPids.Take(MaxBatchSize).ToArray();

        foreach (var pid in pidsToQueue)
        {
            // Skip empty or suspiciously long PIDs
            if (string.IsNullOrWhiteSpace(pid) || pid.Length > 64)
            {
                continue;
            }

            var request = new DownloadRequest
            {
                Id = Guid.NewGuid().ToString("N"),
                Target = pid.Trim()
            };
            await _downloadChannel.Writer.WriteAsync(request, cancellationToken);
        }

        StatusMessage = $"{pidsToQueue.Length} download(s) queued successfully.";
        return RedirectToPage(new { SearchTerm, TypeFilter, ChannelFilter });
    }
}
