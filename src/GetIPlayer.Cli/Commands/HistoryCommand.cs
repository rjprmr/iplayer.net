using System.CommandLine;
using System.CommandLine.Parsing;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GetIPlayer.Cli.Commands;

/// <summary>
/// View and manage download history.
/// </summary>
internal static class HistoryCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("history", "View and manage download history");

        // history list (default behaviour)
        var listCommand = new Command("list", "List download history");
        listCommand.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var historyService = services.GetRequiredService<IHistoryService>();
            var records = await historyService.GetAllAsync(ct);

            if (records.Count == 0)
            {
                Console.WriteLine("No download history.");
                return;
            }

            Console.WriteLine($"Download history ({records.Count} records):");
            Console.WriteLine();

            foreach (var r in records.OrderByDescending(x => x.DownloadedAt))
            {
                var typeLabel = r.Type == ProgrammeType.Tv ? "TV" : "Radio";
                var sizeMb = r.FileSize / 1024.0 / 1024.0;
                Console.WriteLine($"  {r.Pid}  {r.Name} - {r.Episode}");
                Console.WriteLine($"         [{typeLabel}] {r.Channel} | {r.DownloadedAt:yyyy-MM-dd HH:mm} | {sizeMb:F1}MB");
                Console.WriteLine();
            }
        });

        // history clear
        var clearCommand = new Command("clear", "Clear all download history");
        clearCommand.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var historyService = services.GetRequiredService<IHistoryService>();
            await historyService.ClearAsync(ct);
            Console.WriteLine("Download history cleared.");
        });

        // history check
        var pidArg = new Argument<string>("pid") { Description = "PID to check" };
        var checkCommand = new Command("check", "Check if a PID is in history");
        checkCommand.Arguments.Add(pidArg);
        checkCommand.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var pid = result.GetValue(pidArg)!;
            var historyService = services.GetRequiredService<IHistoryService>();
            var exists = await historyService.ExistsAsync(pid, ct);
            Console.WriteLine(exists ? $"{pid}: already downloaded" : $"{pid}: not in history");
        });

        command.Subcommands.Add(listCommand);
        command.Subcommands.Add(clearCommand);
        command.Subcommands.Add(checkCommand);

        return command;
    }
}
