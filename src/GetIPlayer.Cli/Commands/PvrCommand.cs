using System.CommandLine;
using System.CommandLine.Parsing;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GetIPlayer.Cli.Commands;

/// <summary>
/// Manage PVR saved searches and run automated downloads.
/// </summary>
internal static class PvrCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("pvr", "Manage PVR (automated recording) saved searches");

        // pvr run
        var runCommand = new Command("run", "Run all enabled PVR searches and download matches");
        runCommand.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var orchestrator = services.GetRequiredService<PvrOrchestrator>();
            var progress = new Progress<DownloadProgress>(p =>
            {
                Console.Write($"\r  [{p.Phase}] {p.Pid}          ");
            });

            Console.WriteLine("Running PVR searches...");
            var results = await orchestrator.RunAllAsync(progress, ct);
            Console.WriteLine();

            var success = results.Count(r => r.Status == DownloadStatus.Success);
            Console.WriteLine($"PVR complete: {results.Count} programmes processed, {success} downloaded.");
        });

        // pvr add
        var nameArg = new Argument<string>("name") { Description = "Name for this PVR search" };
        var searchArg = new Argument<string>("search-term") { Description = "Search term or regex" };
        var typeOption = new Option<string?>("--type") { Description = "Programme type: tv, radio" };

        var addCommand = new Command("add", "Add a new PVR saved search");
        addCommand.Arguments.Add(nameArg);
        addCommand.Arguments.Add(searchArg);
        addCommand.Options.Add(typeOption);

        addCommand.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var name = result.GetValue(nameArg)!;
            var searchTerm = result.GetValue(searchArg)!;
            var type = result.GetValue(typeOption);

            var orchestrator = services.GetRequiredService<PvrOrchestrator>();
            var programmeType = type?.ToLowerInvariant() switch
            {
                "tv" => (ProgrammeType?)ProgrammeType.Tv,
                "radio" => (ProgrammeType?)ProgrammeType.Radio,
                _ => null
            };

            var search = new PvrSearch
            {
                Name = name,
                SearchTerm = searchTerm,
                Type = programmeType
            };

            await orchestrator.AddSearchAsync(search, ct);
            Console.WriteLine($"PVR search '{name}' added.");
        });

        // pvr list
        var listCommand = new Command("list", "List all PVR saved searches");
        listCommand.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var orchestrator = services.GetRequiredService<PvrOrchestrator>();
            var searches = await orchestrator.ListSearchesAsync(ct);

            if (searches.Count == 0)
            {
                Console.WriteLine("No PVR searches configured.");
                return;
            }

            Console.WriteLine($"PVR searches ({searches.Count}):");
            foreach (var s in searches)
            {
                var status = s.IsEnabled ? "ON " : "OFF";
                var typeStr = s.Type?.ToString() ?? "All";
                Console.WriteLine($"  [{status}] {s.Name}: \"{s.SearchTerm}\" ({typeStr})");
                if (s.LastRunAt.HasValue)
                {
                    Console.WriteLine($"         Last run: {s.LastRunAt.Value:yyyy-MM-dd HH:mm}");
                }
            }
        });

        // pvr delete
        var deleteNameArg = new Argument<string>("name") { Description = "Name of PVR search to delete" };
        var deleteCommand = new Command("delete", "Delete a PVR saved search");
        deleteCommand.Arguments.Add(deleteNameArg);
        deleteCommand.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var name = result.GetValue(deleteNameArg)!;
            var orchestrator = services.GetRequiredService<PvrOrchestrator>();
            await orchestrator.DeleteSearchAsync(name, ct);
            Console.WriteLine($"PVR search '{name}' deleted.");
        });

        command.Subcommands.Add(runCommand);
        command.Subcommands.Add(addCommand);
        command.Subcommands.Add(listCommand);
        command.Subcommands.Add(deleteCommand);

        return command;
    }
}
