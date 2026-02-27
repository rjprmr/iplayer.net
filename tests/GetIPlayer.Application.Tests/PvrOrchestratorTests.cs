using FluentAssertions;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Application.Tests;

public class PvrOrchestratorTests
{
    private readonly Mock<IPvrService> _pvrService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<PvrOrchestrator>> _logger = new();
    private readonly PvrOrchestrator _sut;

    public PvrOrchestratorTests()
    {
        _sut = new PvrOrchestrator(
            _pvrService.Object,
            _cacheService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task RunAllAsync_RefreshesCacheBeforeRunning()
    {
        _pvrService.Setup(x => x.RunAsync(It.IsAny<IProgress<DownloadProgress>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DownloadResult>());

        await _sut.RunAllAsync();

        _cacheService.Verify(x => x.RefreshAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(x => x.RefreshAsync(ProgrammeType.Radio, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAllAsync_DelegatesToPvrService()
    {
        var results = new List<DownloadResult>
        {
            new() { Pid = "b01rryzz", Status = DownloadStatus.Success },
            new() { Pid = "b02rryzz", Status = DownloadStatus.Failed }
        };

        _pvrService.Setup(x => x.RunAsync(It.IsAny<IProgress<DownloadProgress>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var result = await _sut.RunAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddSearchAsync_DelegatesToPvrService()
    {
        var search = new PvrSearch { Name = "test", SearchTerm = "doctor who" };

        await _sut.AddSearchAsync(search);

        _pvrService.Verify(x => x.AddAsync(search, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSearchAsync_WithNull_ThrowsArgumentNullException()
    {
        await _sut.Invoking(s => s.AddSearchAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteSearchAsync_DelegatesToPvrService()
    {
        await _sut.DeleteSearchAsync("test");

        _pvrService.Verify(x => x.DeleteAsync("test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSearchAsync_WithEmptyName_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.DeleteSearchAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListSearchesAsync_DelegatesToPvrService()
    {
        var searches = new List<PvrSearch>
        {
            new() { Name = "s1", SearchTerm = "doctor" },
            new() { Name = "s2", SearchTerm = "eastenders" }
        };

        _pvrService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(searches);

        var result = await _sut.ListSearchesAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetSearchEnabledAsync_DelegatesToPvrService()
    {
        await _sut.SetSearchEnabledAsync("test", false);

        _pvrService.Verify(x => x.SetEnabledAsync("test", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSearchEnabledAsync_WithEmptyName_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.SetSearchEnabledAsync("", true))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PvrOrchestrator(null!, _cacheService.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() => new PvrOrchestrator(_pvrService.Object, null!, _logger.Object));
        Assert.Throws<ArgumentNullException>(() => new PvrOrchestrator(_pvrService.Object, _cacheService.Object, null!));
    }
}
