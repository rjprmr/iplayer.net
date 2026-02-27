using System.CommandLine;
using FluentAssertions;
using GetIPlayer.Application;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GetIPlayer.Cli.Tests;

public class CommandTreeTests
{
    private readonly IServiceProvider _services;

    public CommandTreeTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(Mock.Of<IDownloadService>());
        serviceCollection.AddSingleton(Mock.Of<IHistoryService>());
        serviceCollection.AddSingleton(Mock.Of<ICacheService>());
        serviceCollection.AddSingleton(Mock.Of<IProgrammeService>());
        serviceCollection.AddSingleton(Mock.Of<IPvrService>());
        serviceCollection.AddSingleton(Mock.Of<IStreamService>());
        serviceCollection.AddSingleton(Mock.Of<IFileSystem>());
        serviceCollection.AddSingleton(Mock.Of<Microsoft.Extensions.Logging.ILogger<SearchOrchestrator>>());
        serviceCollection.AddSingleton(Mock.Of<Microsoft.Extensions.Logging.ILogger<PvrOrchestrator>>());
        serviceCollection.AddSingleton(Mock.Of<Microsoft.Extensions.Logging.ILogger<OptionsFileService>>());
        serviceCollection.AddSingleton<SearchOrchestrator>();
        serviceCollection.AddSingleton<PvrOrchestrator>();
        serviceCollection.AddSingleton(sp => new OptionsFileService(
            sp.GetRequiredService<IFileSystem>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OptionsFileService>>(),
            "/tmp/test/options.json"));
        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void SearchCommand_Create_ReturnsValidCommand()
    {
        var command = Commands.SearchCommand.Create(_services);

        command.Should().NotBeNull();
        command.Name.Should().Be("search");
        command.Arguments.Should().Contain(a => a.Name == "search-term");
    }

    [Fact]
    public void GetCommand_Create_ReturnsValidCommand()
    {
        var command = Commands.GetCommand.Create(_services);

        command.Should().NotBeNull();
        command.Name.Should().Be("get");
        command.Arguments.Should().Contain(a => a.Name == "targets");
    }

    [Fact]
    public void HistoryCommand_Create_ReturnsValidCommand()
    {
        var command = Commands.HistoryCommand.Create(_services);

        command.Should().NotBeNull();
        command.Name.Should().Be("history");
        command.Subcommands.Should().Contain(c => c.Name == "list");
        command.Subcommands.Should().Contain(c => c.Name == "clear");
        command.Subcommands.Should().Contain(c => c.Name == "check");
    }

    [Fact]
    public void PvrCommand_Create_ReturnsValidCommand()
    {
        var command = Commands.PvrCommand.Create(_services);

        command.Should().NotBeNull();
        command.Name.Should().Be("pvr");
    }

    [Fact]
    public void InfoCommand_Create_ReturnsValidCommand()
    {
        var command = Commands.InfoCommand.Create(_services);

        command.Should().NotBeNull();
        command.Name.Should().Be("info");
    }

    [Fact]
    public void RefreshCommand_Create_ReturnsValidCommand()
    {
        var command = Commands.RefreshCommand.Create(_services);

        command.Should().NotBeNull();
        command.Name.Should().Be("refresh");
    }

    [Fact]
    public void PrefsCommand_Create_ReturnsValidCommand()
    {
        var command = Commands.PrefsCommand.Create(_services);

        command.Should().NotBeNull();
        command.Name.Should().Be("prefs");
    }
}
