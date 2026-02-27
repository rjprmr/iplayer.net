using FluentAssertions;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Application.Tests;

public class SearchOrchestratorTests
{
    private readonly Mock<IProgrammeService> _programmeService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<SearchOrchestrator>> _logger = new();
    private readonly SearchOrchestrator _sut;

    public SearchOrchestratorTests()
    {
        _sut = new SearchOrchestrator(
            _programmeService.Object,
            _cacheService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task SearchAsync_MatchesByName_ReturnsResults()
    {
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Doctor Who", Episode = "S01E01" },
            new() { Pid = "b02rryzz", Type = ProgrammeType.Tv, Name = "EastEnders", Episode = "Ep1" }
        };

        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(programmes);

        var result = await _sut.SearchAsync("Doctor", [ProgrammeType.Tv]);

        result.Programmes.Should().HaveCount(1);
        result.Programmes[0].Pid.Should().Be("b01rryzz");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_MatchesByEpisode_ReturnsResults()
    {
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Show", Episode = "The Christmas Special" }
        };

        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(programmes);

        var result = await _sut.SearchAsync("Christmas", [ProgrammeType.Tv]);

        result.Programmes.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_MatchesByPid_ReturnsResults()
    {
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Show" }
        };

        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(programmes);

        var result = await _sut.SearchAsync("b01rryzz", [ProgrammeType.Tv]);

        result.Programmes.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmpty()
    {
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Doctor Who" }
        };

        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(programmes);

        var result = await _sut.SearchAsync("Nonexistent", [ProgrammeType.Tv]);

        result.Programmes.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WhenCacheIsStale_RefreshesCache()
    {
        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(true);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Programme>());

        await _sut.SearchAsync("test", [ProgrammeType.Tv]);

        _cacheService.Verify(x => x.RefreshAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenCacheIsFresh_DoesNotRefresh()
    {
        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Programme>());

        await _sut.SearchAsync("test", [ProgrammeType.Tv]);

        _cacheService.Verify(x => x.RefreshAsync(It.IsAny<ProgrammeType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyTypes_SearchesBothTvAndRadio()
    {
        _cacheService.Setup(x => x.IsStaleAsync(It.IsAny<ProgrammeType>())).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(It.IsAny<ProgrammeType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Programme>());

        await _sut.SearchAsync("test", []);

        _cacheService.Verify(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(x => x.GetAllAsync(ProgrammeType.Radio, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithChannelFilter_FiltersResults()
    {
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Show A", Channel = "BBC One" },
            new() { Pid = "b02rryzz", Type = ProgrammeType.Tv, Name = "Show B", Channel = "BBC Two" }
        };

        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(programmes);

        var result = await _sut.SearchAsync("Show", [ProgrammeType.Tv], "BBC One");

        result.Programmes.Should().HaveCount(1);
        result.Programmes[0].Channel.Should().Be("BBC One");
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Doctor Who" }
        };

        _cacheService.Setup(x => x.IsStaleAsync(ProgrammeType.Tv)).ReturnsAsync(false);
        _cacheService.Setup(x => x.GetAllAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(programmes);

        var result = await _sut.SearchAsync("doctor who", [ProgrammeType.Tv]);

        result.Programmes.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_WithNullOrWhiteSpaceSearchTerm_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.SearchAsync("", [ProgrammeType.Tv]))
            .Should().ThrowAsync<ArgumentException>();

        await _sut.Invoking(s => s.SearchAsync("   ", [ProgrammeType.Tv]))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetInfoAsync_DelegatesToProgrammeService()
    {
        var programme = new Programme { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Show" };
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(programme);

        var result = await _sut.GetInfoAsync("b01rryzz");

        result.Should().NotBeNull();
        result!.Pid.Should().Be("b01rryzz");
    }

    [Fact]
    public async Task GetInfoAsync_WhenNotFound_ReturnsNull()
    {
        _programmeService.Setup(x => x.GetByPidAsync("notfound", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Programme?)null);

        var result = await _sut.GetInfoAsync("notfound");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshCacheAsync_RefreshesRequestedTypes()
    {
        await _sut.RefreshCacheAsync([ProgrammeType.Tv]);

        _cacheService.Verify(x => x.RefreshAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(x => x.RefreshAsync(ProgrammeType.Radio, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshCacheAsync_WithEmptyTypes_RefreshesBothTypes()
    {
        await _sut.RefreshCacheAsync([]);

        _cacheService.Verify(x => x.RefreshAsync(ProgrammeType.Tv, It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(x => x.RefreshAsync(ProgrammeType.Radio, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SearchOrchestrator(null!, _cacheService.Object, _logger.Object));
        Assert.Throws<ArgumentNullException>(() => new SearchOrchestrator(_programmeService.Object, null!, _logger.Object));
        Assert.Throws<ArgumentNullException>(() => new SearchOrchestrator(_programmeService.Object, _cacheService.Object, null!));
    }
}
