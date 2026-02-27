using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Tests.Models;

public class DownloadResultTests
{
    [Fact]
    public void DownloadResult_WithRequiredProperties_SetsValues()
    {
        var result = new DownloadResult
        {
            Pid = "b01rryzz",
            Status = DownloadStatus.Success,
            FilePath = "/tmp/test.mp4",
            Mode = "hvf_hls_hd",
            BytesDownloaded = 1024000,
            Duration = TimeSpan.FromMinutes(5)
        };

        result.Pid.Should().Be("b01rryzz");
        result.Status.Should().Be(DownloadStatus.Success);
        result.FilePath.Should().Be("/tmp/test.mp4");
        result.Mode.Should().Be("hvf_hls_hd");
        result.BytesDownloaded.Should().Be(1024000);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void DownloadResult_Failed_HasErrorMessage()
    {
        var result = new DownloadResult
        {
            Pid = "b01rryzz",
            Status = DownloadStatus.Failed,
            ErrorMessage = "Content is geo-blocked"
        };

        result.Status.Should().Be(DownloadStatus.Failed);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.FilePath.Should().BeNull();
    }

    [Fact]
    public void DownloadResult_DefaultValues_AreCorrect()
    {
        var result = new DownloadResult
        {
            Pid = "b01rryzz",
            Status = DownloadStatus.Queued
        };

        result.FilePath.Should().BeNull();
        result.Mode.Should().BeNull();
        result.BytesDownloaded.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
        result.Duration.Should().Be(TimeSpan.Zero);
    }
}
