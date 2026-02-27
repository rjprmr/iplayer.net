using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Exceptions;
using GetIPlayer.Infrastructure.Bbc;

namespace GetIPlayer.Infrastructure.Tests.Bbc;

public class UrlBuilderTests
{
    [Fact]
    public void PlaylistJson_WithValidPid_BuildsCorrectUrl()
    {
        var url = UrlBuilder.PlaylistJson("b01rryzz");
        url.Should().Be("https://www.bbc.co.uk/programmes/b01rryzz/playlist.json");
    }

    [Fact]
    public void PlaylistJson_WithInvalidPid_ThrowsValidationException()
    {
        var act = () => UrlBuilder.PlaylistJson("invalid");
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ProgrammePage_WithValidPid_BuildsCorrectUrl()
    {
        var url = UrlBuilder.ProgrammePage("b01rryzz");
        url.Should().Be("https://www.bbc.co.uk/programmes/b01rryzz");
    }

    [Fact]
    public void ScheduleJson_WithChannelAndDate_BuildsCorrectUrl()
    {
        var date = new DateOnly(2024, 3, 15);
        var url = UrlBuilder.ScheduleJson("bbcone", date);
        url.Should().Be("https://www.bbc.co.uk/bbcone/programmes/schedules/2024/03/15.json");
    }

    [Fact]
    public void ScheduleJson_WithNullChannelId_ThrowsArgumentException()
    {
        var act = () => UrlBuilder.ScheduleJson(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EpisodeGuide_WithValidPid_BuildsCorrectUrl()
    {
        var url = UrlBuilder.EpisodeGuide("b01rryzz");
        url.Should().Contain("/programmes/b01rryzz/episodes/player");
    }

    [Fact]
    public void IPlayerEpisode_WithValidPid_BuildsCorrectUrl()
    {
        var url = UrlBuilder.IPlayerEpisode("b01rryzz");
        url.Should().Be("https://www.bbc.co.uk/iplayer/episode/b01rryzz");
    }

    [Fact]
    public void SoundsPlay_WithValidPid_BuildsCorrectUrl()
    {
        var url = UrlBuilder.SoundsPlay("b01rryzz");
        url.Should().Be("https://www.bbc.co.uk/sounds/play/b01rryzz");
    }

    [Fact]
    public void Search_WithQuery_BuildsCorrectUrl()
    {
        var url = UrlBuilder.Search("doctor who", ProgrammeType.Tv, 1, 25);
        url.Should().StartWith("https://ibl.api.bbc.co.uk/ibl/v1/search");
        url.Should().Contain("q=doctor%20who");
        url.Should().Contain("mediatype=tv");
    }

    [Fact]
    public void Search_WithRadioType_SetsRadioMediaType()
    {
        var url = UrlBuilder.Search("test", ProgrammeType.Radio);
        url.Should().Contain("mediatype=radio");
    }

    [Fact]
    public void Search_WithEmptyQuery_ThrowsArgumentException()
    {
        var act = () => UrlBuilder.Search("", ProgrammeType.Tv);
        act.Should().Throw<ArgumentException>();
    }
}
