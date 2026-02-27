using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Core.Tests.Models;

public class PvrSearchTests
{
    [Fact]
    public void PvrSearch_DefaultValues_AreCorrect()
    {
        var search = new PvrSearch
        {
            Name = "My Search",
            SearchTerm = "doctor who"
        };

        search.Type.Should().BeNull();
        search.ChannelFilter.Should().BeNull();
        search.QualityPreferences.Should().BeEmpty();
        search.DownloadSubtitles.Should().BeFalse();
        search.IsEnabled.Should().BeTrue();
        search.OutputDirectory.Should().BeNull();
        search.LastRunAt.Should().BeNull();
    }

    [Fact]
    public void PvrSearch_WithModification_CreatesNewInstance()
    {
        var original = new PvrSearch
        {
            Name = "My Search",
            SearchTerm = "doctor who",
            IsEnabled = true
        };

        var disabled = original with { IsEnabled = false };

        disabled.IsEnabled.Should().BeFalse();
        original.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void PvrSearch_WithAllProperties_SetsValues()
    {
        var search = new PvrSearch
        {
            Name = "Full Search",
            SearchTerm = "eastenders",
            Type = ProgrammeType.Tv,
            ChannelFilter = "BBC One",
            QualityPreferences = [QualityLevel.Hd, QualityLevel.Sd],
            DownloadSubtitles = true,
            IsEnabled = true,
            OutputDirectory = "/downloads"
        };

        search.Name.Should().Be("Full Search");
        search.Type.Should().Be(ProgrammeType.Tv);
        search.QualityPreferences.Should().HaveCount(2);
        search.DownloadSubtitles.Should().BeTrue();
    }
}
