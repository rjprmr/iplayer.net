using FluentAssertions;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Enums;

namespace GetIPlayer.Core.Tests.Configuration;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();

        settings.ProfileDir.Should().BeEmpty();
        settings.DefaultTypes.Should().BeEquivalentTo([ProgrammeType.Tv]);
        settings.MaxRetries.Should().Be(3);
        settings.CheckForUpdates.Should().BeFalse();
        settings.Download.Should().NotBeNull();
        settings.Proxy.Should().NotBeNull();
        settings.Output.Should().NotBeNull();
    }

    [Fact]
    public void AppSettings_SectionName_IsCorrect()
    {
        AppSettings.SectionName.Should().Be("GetIPlayer");
    }
}

public class DownloadOptionsTests
{
    [Fact]
    public void DownloadOptions_DefaultValues_AreCorrect()
    {
        var options = new DownloadOptions();

        options.TvQuality.Should().BeEquivalentTo(
            [QualityLevel.Hd, QualityLevel.Sd, QualityLevel.Web, QualityLevel.Mobile],
            o => o.WithStrictOrdering());
        options.RadioQuality.Should().BeEquivalentTo(
            [QualityLevel.Hd, QualityLevel.Sd, QualityLevel.Web, QualityLevel.Mobile],
            o => o.WithStrictOrdering());
        options.Subtitles.Should().BeFalse();
        options.Thumbnail.Should().BeTrue();
        options.Tag.Should().BeTrue();
        options.Overwrite.Should().BeFalse();
        options.Force.Should().BeFalse();
        options.MaxConcurrentSegments.Should().Be(4);
        options.Versions.Should().BeEquivalentTo(["original"]);
        options.FfmpegPath.Should().Be("ffmpeg");
        options.AtomicParsleyPath.Should().Be("AtomicParsley");
    }
}

public class OutputOptionsTests
{
    [Fact]
    public void OutputOptions_DefaultValues_AreCorrect()
    {
        var options = new OutputOptions();

        options.OutputDir.Should().BeEmpty();
        options.FilePrefix.Should().Be("{name} - {episode} {pid}");
        options.SubDir.Should().BeEmpty();
        options.AddVersionToFilename.Should().BeTrue();
        options.MaxFilenameLength.Should().Be(200);
    }
}

public class ProxySettingsTests
{
    [Fact]
    public void ProxySettings_DefaultValues_AreCorrect()
    {
        var settings = new ProxySettings();

        settings.Url.Should().BeNull();
        settings.Disabled.Should().BeFalse();
        settings.PartialProxy.Should().BeFalse();
    }
}
