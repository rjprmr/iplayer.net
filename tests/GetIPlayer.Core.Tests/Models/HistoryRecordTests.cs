using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Tests.Models;

public class HistoryRecordTests
{
    [Fact]
    public void HistoryRecord_DefaultValues_AreCorrect()
    {
        var record = new HistoryRecord
        {
            Pid = "b01rryzz",
            Name = "Test Show"
        };

        record.Episode.Should().BeEmpty();
        record.Mode.Should().BeEmpty();
        record.FilePath.Should().BeNull();
        record.FileSize.Should().Be(0);
        record.Channel.Should().BeEmpty();
        record.Version.Should().Be("original");
        record.Duration.Should().Be(TimeSpan.Zero);
        record.ThumbnailUrl.Should().BeNull();
    }

    [Fact]
    public void HistoryRecord_WithAllProperties_SetsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var record = new HistoryRecord
        {
            Pid = "b01rryzz",
            Name = "Doctor Who",
            Episode = "S01E01",
            Type = ProgrammeType.Tv,
            DownloadedAt = now,
            Mode = "hvf_hls_hd",
            FilePath = "/downloads/doctor_who.mp4",
            FileSize = 1_500_000_000,
            Channel = "BBC One",
            Version = "original",
            Duration = TimeSpan.FromMinutes(60)
        };

        record.Pid.Should().Be("b01rryzz");
        record.Name.Should().Be("Doctor Who");
        record.Type.Should().Be(ProgrammeType.Tv);
        record.DownloadedAt.Should().Be(now);
        record.FileSize.Should().Be(1_500_000_000);
    }
}
