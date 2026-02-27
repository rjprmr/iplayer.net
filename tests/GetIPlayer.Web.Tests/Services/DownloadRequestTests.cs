using FluentAssertions;
using GetIPlayer.Web.Services;

namespace GetIPlayer.Web.Tests.Services;

public class DownloadRequestTests
{
    [Fact]
    public void DownloadRequest_DefaultValues_AreCorrect()
    {
        var request = new DownloadRequest
        {
            Id = "abc123",
            Target = "b01rryzz"
        };

        request.ConnectionId.Should().BeNull();
        request.Force.Should().BeFalse();
        request.Subtitles.Should().BeFalse();
        request.Overwrite.Should().BeFalse();
    }

    [Fact]
    public void DownloadRequest_WithAllProperties_SetsCorrectly()
    {
        var request = new DownloadRequest
        {
            Id = "abc123",
            Target = "b01rryzz",
            ConnectionId = "conn-1",
            Force = true,
            Subtitles = true,
            Overwrite = true
        };

        request.Id.Should().Be("abc123");
        request.Target.Should().Be("b01rryzz");
        request.ConnectionId.Should().Be("conn-1");
        request.Force.Should().BeTrue();
        request.Subtitles.Should().BeTrue();
        request.Overwrite.Should().BeTrue();
    }
}
