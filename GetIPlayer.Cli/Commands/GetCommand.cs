using System.CommandLine;
using System.CommandLine.Parsing;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GetIPlayer.Cli.Commands;

/// <summary>
/// Download a programme by PID, index, or URL.
/// Usage: get-iplayer get b0ABC123 [--force] [--subtitles] [--output /path]
/// </summary>
internal static class GetCommand
{
    public static Command Create(IServiceProvider services)
    {
        var targetArg = new Argument<string[]>("targets")
        {
            Description = "PIDs, index numbers, or URLs to download",
            Arity = ArgumentArity.OneOrMore
        };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Force re-download even if in history" };
        var subtitlesOption = new Option<bool>("--subtitles") { Description = "Download subtitles" };
        var overwriteOption = new Option<bool>("--overwrite") { Description = "Overwrite existing files" };

        var command = new Command("get", "Download programmes");
        command.Arguments.Add(targetArg);
        command.Options.Add(forceOption);
        command.Options.Add(subtitlesOption);
        command.Options.Add(overwriteOption);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var targets = result.GetValue(targetArg) ?? [];
            var downloadService = services.GetRequiredService<IDownloadService>();
            var progress = new Progress<DownloadProgress>(p =>
            {
                var pct = p.PercentComplete.HasValue
                    ? $"{p.PercentComplete:F1}%"
                    : $"{p.BytesDownloaded / 1024.0 / 1024.0:F1}MB";
                Console.Write($"\r  [{pct}] {p.Phase}          ");
            });

            foreach (var target in targets)
            {
                Console.WriteLine($"Downloading: {target}");

                DownloadResult downloadResult;
                if (int.TryParse(target, out var index))
                {
                    downloadResult = await downloadService.DownloadByIndexAsync(index, progress, ct);
                }
                else if (target.Contains("bbc.co.uk", StringComparison.OrdinalIgnoreCase) ||
                         target.Contains("bbc.com", StringComparison.OrdinalIgnoreCase))
                {
                    downloadResult = await downloadService.DownloadByUrlAsync(target, progress, ct);
                }
                else
                {
                    downloadResult = await downloadService.DownloadAsync(target, progress, ct);
                }

                Console.WriteLine(); // Clear progress line
                PrintResult(downloadResult);
            }
        });

        return command;
    }

    private static void PrintResult(DownloadResult result)
    {
        var status = result.Status switch
        {
            DownloadStatus.Success => "OK",
            DownloadStatus.Skipped => "SKIP",
            DownloadStatus.Cancelled => "CANCELLED",
            DownloadStatus.Unavailable => "UNAVAILABLE",
            _ => "FAIL"
        };

        if (result.Status == DownloadStatus.Success)
        {
            var sizeMb = result.BytesDownloaded / 1024.0 / 1024.0;
            Console.WriteLine($"  [{status}] {result.Pid} -> {result.FilePath} ({sizeMb:F1}MB)");
        }
        else
        {
            Console.WriteLine($"  [{status}] {result.Pid}: {result.ErrorMessage}");
        }
    }
}
