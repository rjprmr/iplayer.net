using System.Text.Json;
using FluentAssertions;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Infrastructure.Tests.Persistence;

public class OptionsFileServiceTests
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<ILogger<OptionsFileService>> _logger = new();
    private readonly string _optionsPath = "/tmp/test/options.json";
    private readonly OptionsFileService _sut;

    public OptionsFileServiceTests()
    {
        _sut = new OptionsFileService(_fileSystem.Object, _logger.Object, _optionsPath);
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        _fileSystem.Setup(x => x.FileExists(_optionsPath)).Returns(false);

        var result = await _sut.LoadAsync();

        result.Should().NotBeNull();
        result.MaxRetries.Should().Be(3);
    }

    [Fact]
    public async Task LoadAsync_WithValidJson_ReturnsDeserializedSettings()
    {
        var settings = new AppSettings { MaxRetries = 5, ProfileDir = "/custom" };
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _fileSystem.Setup(x => x.FileExists(_optionsPath)).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(_optionsPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        var result = await _sut.LoadAsync();

        result.MaxRetries.Should().Be(5);
        result.ProfileDir.Should().Be("/custom");
    }

    [Fact]
    public async Task LoadAsync_WithCorruptJson_ReturnsDefaultSettings()
    {
        _fileSystem.Setup(x => x.FileExists(_optionsPath)).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(_optionsPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync("not valid json {{{");

        var result = await _sut.LoadAsync();

        result.Should().NotBeNull();
        result.MaxRetries.Should().Be(3);
    }

    [Fact]
    public async Task SaveAsync_SerializesAndWritesToFile()
    {
        string? savedJson = null;
        _fileSystem.Setup(x => x.WriteAllTextAsync(_optionsPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, json, _) => savedJson = json)
            .Returns(Task.CompletedTask);

        var settings = new AppSettings { MaxRetries = 10 };

        await _sut.SaveAsync(settings);

        savedJson.Should().NotBeNull();
        savedJson.Should().Contain("maxRetries");
    }

    [Fact]
    public async Task SaveAsync_CreatesDirectoryIfNeeded()
    {
        _fileSystem.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.SaveAsync(new AppSettings());

        _fileSystem.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithNull_ThrowsArgumentNullException()
    {
        await _sut.Invoking(s => s.SaveAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OptionsFileService(null!, _logger.Object, _optionsPath));
        Assert.Throws<ArgumentNullException>(() => new OptionsFileService(_fileSystem.Object, null!, _optionsPath));
        Assert.Throws<ArgumentNullException>(() => new OptionsFileService(_fileSystem.Object, _logger.Object, null!));
    }
}
