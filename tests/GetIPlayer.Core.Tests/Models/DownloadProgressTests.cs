using FluentAssertions;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Tests.Models;

public class DownloadProgressTests
{
    [Fact]
    public void PercentComplete_WhenTotalBytesIsPositive_ReturnsCorrectPercentage()
    {
        var progress = new DownloadProgress
        {
            Pid = "b01rryzz",
            TotalBytes = 1000,
            BytesDownloaded = 250
        };

        progress.PercentComplete.Should().Be(25.0);
    }

    [Fact]
    public void PercentComplete_WhenTotalBytesIsZero_ReturnsNull()
    {
        var progress = new DownloadProgress
        {
            Pid = "b01rryzz",
            TotalBytes = 0,
            BytesDownloaded = 250
        };

        progress.PercentComplete.Should().BeNull();
    }

    [Fact]
    public void PercentComplete_WhenFullyDownloaded_Returns100()
    {
        var progress = new DownloadProgress
        {
            Pid = "b01rryzz",
            TotalBytes = 500,
            BytesDownloaded = 500
        };

        progress.PercentComplete.Should().Be(100.0);
    }

    [Fact]
    public void PercentComplete_WhenNothingDownloaded_ReturnsZero()
    {
        var progress = new DownloadProgress
        {
            Pid = "b01rryzz",
            TotalBytes = 500,
            BytesDownloaded = 0
        };

        progress.PercentComplete.Should().Be(0.0);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        var progress = new DownloadProgress { Pid = "b01rryzz" };

        progress.TotalBytes.Should().Be(0);
        progress.BytesDownloaded.Should().Be(0);
        progress.BytesPerSecond.Should().Be(0);
        progress.EstimatedTimeRemaining.Should().BeNull();
        progress.Phase.Should().BeEmpty();
    }
}
