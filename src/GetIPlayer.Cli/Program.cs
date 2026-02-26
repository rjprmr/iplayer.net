using System.CommandLine;
using System.Globalization;
using GetIPlayer.Application;
using GetIPlayer.Cli.Commands;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Infrastructure;
using GetIPlayer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog console logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        formatProvider: CultureInfo.InvariantCulture,
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Build DI container
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(Log.Logger);
});

// Load settings (or use defaults)
var optionsService = new OptionsFileService(
    new GetIPlayer.Infrastructure.FileSystem.SafeFileSystem(
        services.BuildServiceProvider().GetRequiredService<ILogger<GetIPlayer.Infrastructure.FileSystem.SafeFileSystem>>()),
    services.BuildServiceProvider().GetRequiredService<ILogger<OptionsFileService>>(),
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".get_iplayer", "options.json"));

AppSettings settings;
try
{
    settings = await optionsService.LoadAsync();
}
catch
{
    settings = new AppSettings();
}

services.AddGetIPlayerServices(settings);

// Register Application orchestrators
services.AddSingleton<IDownloadService, DownloadOrchestrator>();
services.AddSingleton<SearchOrchestrator>();
services.AddSingleton<PvrOrchestrator>();

var serviceProvider = services.BuildServiceProvider();

// Build command tree
var rootCommand = new RootCommand("get_iplayer - BBC iPlayer/Sounds downloader");
rootCommand.Subcommands.Add(SearchCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(GetCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(PvrCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(HistoryCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(InfoCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(RefreshCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(PrefsCommand.Create(serviceProvider));

// Add global options
var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose/debug logging", Recursive = true };
rootCommand.Options.Add(verboseOption);

var profileDirOption = new Option<string?>("--profile-dir") { Description = "Override profile directory", Recursive = true };
rootCommand.Options.Add(profileDirOption);

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
