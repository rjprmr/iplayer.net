using FluentAssertions;
using GetIPlayer.Application;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Pages;
using GetIPlayer.Web.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Web.Tests.Pages;

public class IndexModelTests
{
    private readonly Mock<IHistoryService> _historyService = new();
    private readonly Mock<IPvrService> _pvrService = new();
    private readonly PvrOrchestrator _pvrOrchestrator;
    private readonly DownloadTracker _tracker = new();
    private readonly IndexModel _sut;

    public IndexModelTests()
    {
        // Use a real PvrOrchestrator (sealed class) with mocked dependencies
        _pvrService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PvrSearch>());

        _pvrOrchestrator = new PvrOrchestrator(
            _pvrService.Object,
            Mock.Of<ICacheService>(),
            Mock.Of<ILogger<PvrOrchestrator>>());

        _sut = new IndexModel(_historyService.Object, _pvrOrchestrator, _tracker);
    }

    [Fact]
    public async Task OnGetAsync_LoadsDashboardData()
    {
        var history = new List<HistoryRecord>
        {
            new() { Pid = "b01rryzz", Name = "Show 1", DownloadedAt = DateTimeOffset.UtcNow },
            new() { Pid = "b02rryzz", Name = "Show 2", DownloadedAt = DateTimeOffset.UtcNow.AddHours(-1) }
        };

        _historyService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.HistoryCount.Should().Be(2);
        _sut.RecentHistory.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task OnGetAsync_WithNoHistory_ShowsZeroCounts()
    {
        _historyService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HistoryRecord>());

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.HistoryCount.Should().Be(0);
        _sut.RecentHistory.Should().BeEmpty();
        _sut.PvrSearchCount.Should().Be(0);
    }

    [Fact]
    public async Task OnGetAsync_TracksActiveDownloads()
    {
        _historyService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HistoryRecord>());

        _tracker.UpdateProgress("dl-1", new DownloadProgress { Pid = "b01rryzz" });

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.ActiveDownloadCount.Should().Be(1);
        _sut.ActiveDownloads.Should().HaveCount(1);
    }
}
