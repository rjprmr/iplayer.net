using System.CommandLine;
using System.CommandLine.Parsing;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GetIPlayer.Cli.Commands;

/// <summary>
/// Search for BBC programmes.
/// Usage: get-iplayer search "query" [--type tv|radio] [--channel bbc_one]
/// </summary>
internal static class SearchCommand
{
    public static Command Create(IServiceProvider services)
    {
        var searchTermArg = new Argument<string>("search-term") { Description = "Search term or regex pattern" };
        var typeOption = new Option<string[]>("--type", "-t") { Description = "Programme type(s): tv, radio" };
        var channelOption = new Option<string?>("--channel") { Description = "Filter by channel name" };

        var command = new Command("search", "Search for BBC programmes");
        command.Arguments.Add(searchTermArg);
        command.Options.Add(typeOption);
        command.Options.Add(channelOption);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var searchTerm = result.GetValue(searchTermArg)!;
            var types = result.GetValue(typeOption) ?? [];
            var channel = result.GetValue(channelOption);

            var orchestrator = services.GetRequiredService<SearchOrchestrator>();
            var programmeTypes = ParseTypes(types);

            var searchResult = await orchestrator.SearchAsync(searchTerm, programmeTypes, channel, ct);

            if (searchResult.Programmes.Count == 0)
            {
                Console.WriteLine("No programmes found.");
                return;
            }

            Console.WriteLine($"Found {searchResult.TotalCount} programme(s):");
            Console.WriteLine();

            foreach (var p in searchResult.Programmes)
            {
                var typeLabel = p.Type == ProgrammeType.Tv ? "TV" : "Radio";
                Console.WriteLine($"  {p.Index,5}: {p.Name} - {p.Episode}");
                Console.WriteLine($"         [{typeLabel}] {p.Channel} | {p.Pid} | {p.Duration}");
                if (!string.IsNullOrEmpty(p.Description))
                {
                    var desc = p.Description.Length > 80 ? string.Concat(p.Description.AsSpan(0, 77), "...") : p.Description;
                    Console.WriteLine($"         {desc}");
                }

                Console.WriteLine();
            }
        });

        return command;
    }

    private static ProgrammeType[] ParseTypes(string[] types)
    {
        if (types.Length == 0)
        {
            return [ProgrammeType.Tv, ProgrammeType.Radio];
        }

        return types
            .Select(t => t.ToLowerInvariant() switch
            {
                "tv" => ProgrammeType.Tv,
                "radio" => ProgrammeType.Radio,
                _ => ProgrammeType.Tv
            })
            .Distinct()
            .ToArray();
    }
}
