using System.Text.Json;
using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Infrastructure.Tests.Persistence;

public class PvrFileServiceTests
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<IDownloadService> _downloadService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<PvrFileService>> _logger = new();
    private readonly string _pvrDir = "/tmp/test/pvr";
    private readonly PvrFileService _sut;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public PvrFileServiceTests()
    {
        _sut = new PvrFileService(
            _fileSystem.Object,
            _downloadService.Object,
            _cacheService.Object,
            _logger.Object,
            _pvrDir);
    }

    [Fact]
    public async Task AddAsync_SavesSearchToFile()
    {
        _fileSystem.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _fileSystem.Setup(x => x.SafeCombinePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((b, r) => Path.Combine(b, r));

        var search = new PvrSearch { Name = "test-search", SearchTerm = "doctor who" };

        await _sut.AddAsync(search);

        _fileSystem.Verify(x => x.WriteAllTextAsync(
            It.Is<string>(p => p.Contains("test-search")),
            It.Is<string>(j => j.Contains("doctor who")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithNull_ThrowsArgumentNullException()
    {
        await _sut.Invoking(s => s.AddAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_DeletesFile()
    {
        _fileSystem.Setup(x => x.SafeCombinePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((b, r) => Path.Combine(b, r));
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

        await _sut.DeleteAsync("test-search");

        _fileSystem.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        _fileSystem.Setup(x => x.SafeCombinePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((b, r) => Path.Combine(b, r));
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

        await _sut.Invoking(s => s.DeleteAsync("nonexistent"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_WithEmptyName_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.DeleteAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAllAsync_WhenDirectoryDoesNotExist_ReturnsEmpty()
    {
        _fileSystem.Setup(x => x.DirectoryExists(_pvrDir)).Returns(false);

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithValidFiles_ReturnsSearches()
    {
        var search = new PvrSearch { Name = "test", SearchTerm = "doctor" };
        var json = JsonSerializer.Serialize(search, JsonOptions);

        _fileSystem.Setup(x => x.DirectoryExists(_pvrDir)).Returns(true);
        _fileSystem.Setup(x => x.GetFiles(_pvrDir, "*.json"))
            .Returns(new[] { "/tmp/test/pvr/test.json" });
        _fileSystem.Setup(x => x.ReadAllTextAsync("/tmp/test/pvr/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].SearchTerm.Should().Be("doctor");
    }

    [Fact]
    public async Task GetByNameAsync_WhenFound_ReturnsSearch()
    {
        var search = new PvrSearch { Name = "test", SearchTerm = "eastenders" };
        var json = JsonSerializer.Serialize(search, JsonOptions);

        _fileSystem.Setup(x => x.SafeCombinePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((b, r) => Path.Combine(b, r));
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        var result = await _sut.GetByNameAsync("test");

        result.Should().NotBeNull();
        result!.SearchTerm.Should().Be("eastenders");
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotFound_ReturnsNull()
    {
        _fileSystem.Setup(x => x.SafeCombinePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((b, r) => Path.Combine(b, r));
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

        var result = await _sut.GetByNameAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetEnabledAsync_WhenSearchExists_UpdatesEnabled()
    {
        var search = new PvrSearch { Name = "test", SearchTerm = "doctor", IsEnabled = true };
        var json = JsonSerializer.Serialize(search, JsonOptions);

        _fileSystem.Setup(x => x.SafeCombinePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((b, r) => Path.Combine(b, r));
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);
        _fileSystem.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.SetEnabledAsync("test", false);

        _fileSystem.Verify(x => x.WriteAllTextAsync(
            It.IsAny<string>(),
            It.Is<string>(j => j.Contains("false")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithEnabledSearches_DownloadsMatches()
    {
        var search = new PvrSearch { Name = "test", SearchTerm = "doctor", IsEnabled = true, Type = ProgrammeType.Tv };
        var searchJson = JsonSerializer.Serialize(search, JsonOptions);

        _fileSystem.Setup(x => x.DirectoryExists(_pvrDir)).Returns(true);
        _fileSystem.Setup(x => x.GetFiles(_pvrDir, "*.json"))
            .Returns(new[] { "/tmp/test/pvr/test.json" });
        _fileSystem.Setup(x => x.ReadAllTextAsync("/tmp/test/pvr/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchJson);
        _fileSystem.Setup(x => x.SafeCombinePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((b, r) => Path.Combine(b, r));
        _fileSystem.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

        var programme = new Programme { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Doctor Who", Episode = "S01E01" };
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Programme> { programme });

        _downloadService.Setup(x => x.DownloadAsync("b01rryzz", It.IsAny<IProgress<DownloadProgress>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadResult { Pid = "b01rryzz", Status = DownloadStatus.Success });

        var results = await _sut.RunAsync();

        results.Should().HaveCount(1);
        results[0].Status.Should().Be(DownloadStatus.Success);
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PvrFileService(null!, _downloadService.Object, _cacheService.Object, _logger.Object, _pvrDir));
        Assert.Throws<ArgumentNullException>(() => new PvrFileService(_fileSystem.Object, null!, _cacheService.Object, _logger.Object, _pvrDir));
        Assert.Throws<ArgumentNullException>(() => new PvrFileService(_fileSystem.Object, _downloadService.Object, null!, _logger.Object, _pvrDir));
        Assert.Throws<ArgumentNullException>(() => new PvrFileService(_fileSystem.Object, _downloadService.Object, _cacheService.Object, null!, _pvrDir));
        Assert.Throws<ArgumentNullException>(() => new PvrFileService(_fileSystem.Object, _downloadService.Object, _cacheService.Object, _logger.Object, null!));
    }
}
