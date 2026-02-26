using System.CommandLine;
using System.CommandLine.Parsing;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GetIPlayer.Cli.Commands;

/// <summary>
/// View and set application preferences.
/// </summary>
internal static class PrefsCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("prefs", "View and set application preferences");

        // prefs show
        var showCommand = new Command("show", "Show current preferences");
        showCommand.SetAction(async (ParseResult _, CancellationToken ct) =>
        {
            var optionsService = services.GetRequiredService<OptionsFileService>();
            var settings = await optionsService.LoadAsync(ct);

            Console.WriteLine("Current preferences:");
            Console.WriteLine($"  Profile dir:       {(string.IsNullOrEmpty(settings.ProfileDir) ? "(default)" : settings.ProfileDir)}");
            Console.WriteLine($"  Output dir:        {settings.Output.OutputDir ?? Environment.CurrentDirectory}");
            Console.WriteLine($"  File prefix:       {settings.Output.FilePrefix}");
            Console.WriteLine($"  TV quality:        {string.Join(", ", settings.Download.TvQuality)}");
            Console.WriteLine($"  Radio quality:     {string.Join(", ", settings.Download.RadioQuality)}");
            Console.WriteLine($"  Subtitles:         {settings.Download.Subtitles}");
            Console.WriteLine($"  Thumbnail:         {settings.Download.Thumbnail}");
            Console.WriteLine($"  Tag:               {settings.Download.Tag}");
            Console.WriteLine($"  Force:             {settings.Download.Force}");
            Console.WriteLine($"  Overwrite:         {settings.Download.Overwrite}");
            Console.WriteLine($"  Versions:          {string.Join(", ", settings.Download.Versions)}");
            Console.WriteLine($"  Max retries:       {settings.MaxRetries}");

            if (!string.IsNullOrEmpty(settings.Proxy.Url))
            {
                Console.WriteLine($"  Proxy:             {settings.Proxy.Url}");
            }

            if (!string.IsNullOrEmpty(settings.Download.FfmpegPath))
            {
                Console.WriteLine($"  ffmpeg:            {settings.Download.FfmpegPath}");
            }
        });

        // prefs set
        var keyArg = new Argument<string>("key") { Description = "Setting key" };
        var valueArg = new Argument<string>("value") { Description = "Setting value" };
        var setCommand = new Command("set", "Set a preference value");
        setCommand.Arguments.Add(keyArg);
        setCommand.Arguments.Add(valueArg);

        setCommand.SetAction(async (ParseResult result, CancellationToken ct) =>
        {
            var key = result.GetValue(keyArg)!;
            var value = result.GetValue(valueArg)!;

            var optionsService = services.GetRequiredService<OptionsFileService>();
            var settings = await optionsService.LoadAsync(ct);

            ApplySetting(settings, key, value);
            await optionsService.SaveAsync(settings, ct);
            Console.WriteLine($"Preference '{key}' set to '{value}'.");
        });

        command.Subcommands.Add(showCommand);
        command.Subcommands.Add(setCommand);

        return command;
    }

    private static void ApplySetting(AppSettings settings, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "output-dir" or "outputdir":
                settings.Output.OutputDir = value;
                break;
            case "file-prefix" or "fileprefix":
                settings.Output.FilePrefix = value;
                break;
            case "subtitles":
                settings.Download.Subtitles = bool.Parse(value);
                break;
            case "thumbnail":
                settings.Download.Thumbnail = bool.Parse(value);
                break;
            case "tag":
                settings.Download.Tag = bool.Parse(value);
                break;
            case "force":
                settings.Download.Force = bool.Parse(value);
                break;
            case "overwrite":
                settings.Download.Overwrite = bool.Parse(value);
                break;
            case "proxy":
                settings.Proxy.Url = value;
                break;
            case "ffmpeg" or "ffmpeg-path":
                settings.Download.FfmpegPath = value;
                break;
            default:
                throw new ArgumentException($"Unknown preference key: {key}");
        }
    }
}
