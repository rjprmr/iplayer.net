using System.CommandLine;
using System.CommandLine.Parsing;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GetIPlayer.Cli.Commands;

/// <summary>
/// Refresh the programme cache.
/// </summary>
internal static class RefreshCommand
{
    public static Command Create(IServiceProvider services)
    {
        var typeOption = new Option<string[]>("--type", "-t") { Description = "Programme type(s): tv, radio" };

        var command = new Command("refresh", "Refresh the programme cache");
        command.Options.Add(typeOption);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var types = result.GetValue(typeOption) ?? [];
            var orchestrator = services.GetRequiredService<SearchOrchestrator>();
            var programmeTypes = types.Length == 0
                ? new[] { ProgrammeType.Tv, ProgrammeType.Radio }
                : types.Select(t => string.Equals(t, "radio", StringComparison.OrdinalIgnoreCase)
                    ? ProgrammeType.Radio
                    : ProgrammeType.Tv)
                       .Distinct()
                       .ToArray();

            Console.WriteLine("Refreshing programme cache...");
            await orchestrator.RefreshCacheAsync(programmeTypes, ct);
            Console.WriteLine("Cache refresh complete.");
        });

        return command;
    }
}
