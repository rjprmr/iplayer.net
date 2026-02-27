using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Services;

namespace GetIPlayer.Web.Tests.Services;

public class DownloadTrackerTests
{
    private readonly DownloadTracker _sut = new();

    [Fact]
    public void UpdateProgress_AddsActiveDownload()
    {
        var progress = new DownloadProgress
        {
            Pid = "b01rryzz",
            Phase = "Downloading",
            BytesDownloaded = 500,
            TotalBytes = 1000
        };

        _sut.UpdateProgress("dl-1", progress);

        var active = _sut.GetActiveDownloads();
        active.Should().HaveCount(1);
        active[0].Pid.Should().Be("b01rryzz");
        active[0].Phase.Should().Be("Downloading");
    }

    [Fact]
    public void UpdateProgress_UpdatesExistingDownload()
    {
        _sut.UpdateProgress("dl-1", new DownloadProgress { Pid = "b01rryzz", BytesDownloaded = 100, TotalBytes = 1000 });
        _sut.UpdateProgress("dl-1", new DownloadProgress { Pid = "b01rryzz", BytesDownloaded = 500, TotalBytes = 1000 });

        var active = _sut.GetActiveDownloads();
        active.Should().HaveCount(1);
        active[0].BytesDownloaded.Should().Be(500);
    }

    [Fact]
    public void MarkCompleted_RemovesFromActiveAndAddsToCompleted()
    {
        var progress = new DownloadProgress { Pid = "b01rryzz" };
        _sut.UpdateProgress("dl-1", progress);

        var result = new DownloadResult { Pid = "b01rryzz", Status = DownloadStatus.Success };
        _sut.MarkCompleted("dl-1", result);

        _sut.GetActiveDownloads().Should().BeEmpty();
        _sut.GetRecentCompleted().Should().HaveCount(1);
        _sut.GetRecentCompleted()[0].Status.Should().Be(DownloadStatus.Success);
    }

    [Fact]
    public void GetRecentCompleted_ReturnsInReverseChronologicalOrder()
    {
        _sut.MarkCompleted("dl-1", new DownloadResult { Pid = "first", Status = DownloadStatus.Success });
        _sut.MarkCompleted("dl-2", new DownloadResult { Pid = "second", Status = DownloadStatus.Success });

        var completed = _sut.GetRecentCompleted();

        completed[0].Pid.Should().Be("second");
        completed[1].Pid.Should().Be("first");
    }

    [Fact]
    public void GetActiveDownloads_WithMultipleDownloads_ReturnsAll()
    {
        _sut.UpdateProgress("dl-1", new DownloadProgress { Pid = "b01rryzz" });
        _sut.UpdateProgress("dl-2", new DownloadProgress { Pid = "b02rryzz" });

        _sut.GetActiveDownloads().Should().HaveCount(2);
    }

    [Fact]
    public void GetActiveDownloads_WhenEmpty_ReturnsEmptyList()
    {
        _sut.GetActiveDownloads().Should().BeEmpty();
    }

    [Fact]
    public void GetRecentCompleted_WhenEmpty_ReturnsEmptyList()
    {
        _sut.GetRecentCompleted().Should().BeEmpty();
    }

    [Fact]
    public void MarkCompleted_WithFailedStatus_TracksError()
    {
        var result = new DownloadResult
        {
            Pid = "b01rryzz",
            Status = DownloadStatus.Failed,
            ErrorMessage = "Geo-blocked"
        };

        _sut.MarkCompleted("dl-1", result);

        var completed = _sut.GetRecentCompleted();
        completed[0].ErrorMessage.Should().Be("Geo-blocked");
    }
}
