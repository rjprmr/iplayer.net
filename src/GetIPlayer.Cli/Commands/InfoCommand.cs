using System.CommandLine;
using System.CommandLine.Parsing;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GetIPlayer.Cli.Commands;

/// <summary>
/// Display detailed information about a programme.
/// </summary>
internal static class InfoCommand
{
    public static Command Create(IServiceProvider services)
    {
        var pidArg = new Argument<string>("pid") { Description = "BBC programme PID" };

        var command = new Command("info", "Show detailed programme information");
        command.Arguments.Add(pidArg);

        command.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var pid = result.GetValue(pidArg)!;
            var orchestrator = services.GetRequiredService<SearchOrchestrator>();
            var programme = await orchestrator.GetInfoAsync(pid, ct);

            if (programme is null)
            {
                Console.WriteLine($"Programme {pid} not found.");
                return;
            }

            var typeLabel = programme.Type == ProgrammeType.Tv ? "TV" : "Radio";

            Console.WriteLine($"Name:        {programme.Name}");
            Console.WriteLine($"Episode:     {programme.Episode}");
            Console.WriteLine($"PID:         {programme.Pid}");
            Console.WriteLine($"Type:        {typeLabel}");
            Console.WriteLine($"Channel:     {programme.Channel}");
            Console.WriteLine($"Duration:    {programme.Duration}");
            Console.WriteLine($"Description: {programme.Description}");

            if (programme.FirstBroadcast.HasValue)
            {
                Console.WriteLine($"First Aired: {programme.FirstBroadcast.Value:yyyy-MM-dd}");
            }

            if (programme.ExpiresAt.HasValue)
            {
                Console.WriteLine($"Expires:     {programme.ExpiresAt.Value:yyyy-MM-dd}");
            }

            Console.WriteLine($"Web URL:     {programme.WebUrl}");

            if (!string.IsNullOrEmpty(programme.ThumbnailUrl))
            {
                Console.WriteLine($"Thumbnail:   {programme.ThumbnailUrl}");
            }

            if (programme.Versions.Count > 0)
            {
                Console.WriteLine($"Versions:    {string.Join(", ", programme.Versions.Select(v => v.Name))}");
            }
        });

        return command;
    }
}
