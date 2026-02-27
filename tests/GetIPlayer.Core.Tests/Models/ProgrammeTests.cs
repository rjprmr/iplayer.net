using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Tests.Models;

public class ProgrammeTests
{
    [Fact]
    public void Programme_DefaultValues_AreCorrect()
    {
        var programme = new Programme
        {
            Pid = "b01rryzz",
            Type = ProgrammeType.Tv,
            Name = "Test Show"
        };

        programme.Episode.Should().BeEmpty();
        programme.Description.Should().BeEmpty();
        programme.Channel.Should().BeEmpty();
        programme.ThumbnailUrl.Should().BeEmpty();
        programme.WebUrl.Should().BeEmpty();
        programme.Duration.Should().Be(TimeSpan.Zero);
        programme.FirstBroadcast.Should().BeNull();
        programme.AvailableFrom.Should().BeNull();
        programme.ExpiresAt.Should().BeNull();
        programme.Versions.Should().BeEmpty();
        programme.Index.Should().Be(0);
        programme.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Programme_WithRecord_SupportsEquality()
    {
        var p1 = new Programme { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Show" };
        var p2 = new Programme { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Show" };

        p1.Should().Be(p2);
    }

    [Fact]
    public void Programme_WithModification_CreatesNewInstance()
    {
        var original = new Programme { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Show", Index = 1 };
        var modified = original with { Index = 42 };

        modified.Index.Should().Be(42);
        original.Index.Should().Be(1);
        modified.Pid.Should().Be(original.Pid);
    }
}
