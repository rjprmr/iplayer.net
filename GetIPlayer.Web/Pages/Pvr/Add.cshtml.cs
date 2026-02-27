using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GetIPlayer.Web.Pages.Pvr;

public sealed class AddModel : PageModel
{
    private readonly PvrOrchestrator _pvr;

    public AddModel(PvrOrchestrator pvr) => _pvr = pvr;

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty]
    public string? TypeFilter { get; set; }

    [BindProperty]
    public string? ChannelFilter { get; set; }

    [BindProperty]
    public bool DownloadSubtitles { get; set; }

    [BindProperty]
    public string? OutputDirectory { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(SearchTerm))
        {
            ErrorMessage = "Name and Search Term are required.";
            return Page();
        }

        ProgrammeType? type = TypeFilter?.ToUpperInvariant() switch
        {
            "TV" => ProgrammeType.Tv,
            "RADIO" => ProgrammeType.Radio,
            _ => null
        };

        var search = new PvrSearch
        {
            Name = Name.Trim(),
            SearchTerm = SearchTerm.Trim(),
            Type = type,
            ChannelFilter = string.IsNullOrWhiteSpace(ChannelFilter) ? null : ChannelFilter.Trim(),
            DownloadSubtitles = DownloadSubtitles,
            OutputDirectory = string.IsNullOrWhiteSpace(OutputDirectory) ? null : OutputDirectory.Trim(),
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _pvr.AddSearchAsync(search, cancellationToken);

        StatusMessage = $"PVR search '{Name}' created successfully.";
        return RedirectToPage("/Pvr/Index");
    }
}
