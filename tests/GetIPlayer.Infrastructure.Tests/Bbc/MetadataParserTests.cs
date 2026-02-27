using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Infrastructure.Bbc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Infrastructure.Tests.Bbc;

public class MetadataParserTests
{
    private readonly MetadataParser _sut;

    public MetadataParserTests()
    {
        _sut = new MetadataParser(Mock.Of<ILogger<MetadataParser>>());
    }

    [Fact]
    public void ParseProgrammeJson_WithValidJson_ReturnsProgramme()
    {
        var json = """
        {
            "programme": {
                "pid": "b01rryzz",
                "title": "Doctor Who",
                "display_title": {
                    "title": "Doctor Who",
                    "subtitle": "The Day of the Doctor"
                },
                "short_synopsis": "A short synopsis",
                "medium_synopsis": "A medium synopsis",
                "ownership": {
                    "service": {
                        "title": "BBC One"
                    }
                },
                "duration": 3600,
                "first_broadcast_date": "2013-11-23T19:50:00Z"
            }
        }
        """;

        var result = _sut.ParseProgrammeJson(json, ProgrammeType.Tv);

        result.Should().NotBeNull();
        result!.Pid.Should().Be("b01rryzz");
        result.Name.Should().Be("Doctor Who");
        result.Episode.Should().Be("The Day of the Doctor");
        result.Description.Should().Be("A medium synopsis");
        result.Channel.Should().Be("BBC One");
        result.Duration.Should().Be(TimeSpan.FromSeconds(3600));
        result.Type.Should().Be(ProgrammeType.Tv);
    }

    [Fact]
    public void ParseProgrammeJson_WithNoProgrammeElement_ReturnsNull()
    {
        var json = """{ "data": {} }""";

        var result = _sut.ParseProgrammeJson(json, ProgrammeType.Tv);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseProgrammeJson_WithInvalidJson_ReturnsNull()
    {
        var result = _sut.ParseProgrammeJson("not json at all", ProgrammeType.Tv);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseProgrammeJson_WithEmptyString_ThrowsArgumentException()
    {
        var act = () => _sut.ParseProgrammeJson("", ProgrammeType.Tv);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseProgramme_Static_WithValidJson_ReturnsProgramme()
    {
        var json = """
        {
            "programme": {
                "pid": "b01rryzz",
                "title": "Test"
            }
        }
        """;

        var result = MetadataParser.ParseProgramme(json, ProgrammeType.Tv);

        result.Should().NotBeNull();
        result!.Pid.Should().Be("b01rryzz");
    }

    [Fact]
    public void ParseProgramme_Static_WithNullOrWhitespace_ReturnsNull()
    {
        MetadataParser.ParseProgramme(null!, ProgrammeType.Tv).Should().BeNull();
        MetadataParser.ParseProgramme("", ProgrammeType.Tv).Should().BeNull();
        MetadataParser.ParseProgramme("   ", ProgrammeType.Tv).Should().BeNull();
    }

    [Fact]
    public void ParseProgramme_Static_WithInvalidJson_ReturnsNull()
    {
        MetadataParser.ParseProgramme("not json", ProgrammeType.Tv).Should().BeNull();
    }

    [Fact]
    public void ParseProgrammeJson_WithMissingPid_ReturnsNull()
    {
        var json = """
        {
            "programme": {
                "title": "No PID"
            }
        }
        """;

        var result = _sut.ParseProgrammeJson(json, ProgrammeType.Tv);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseProgrammeJson_TvProgramme_SetsIPlayerUrl()
    {
        var json = """
        {
            "programme": {
                "pid": "b01rryzz",
                "title": "Test"
            }
        }
        """;

        var result = _sut.ParseProgrammeJson(json, ProgrammeType.Tv);

        result!.WebUrl.Should().Contain("iplayer/episode");
    }

    [Fact]
    public void ParseProgrammeJson_RadioProgramme_SetsSoundsUrl()
    {
        var json = """
        {
            "programme": {
                "pid": "b01rryzz",
                "title": "Test"
            }
        }
        """;

        var result = _sut.ParseProgrammeJson(json, ProgrammeType.Radio);

        result!.WebUrl.Should().Contain("sounds/play");
    }

    [Fact]
    public void ParseProgrammeJson_WithImage_SetsThumbnailUrl()
    {
        var json = """
        {
            "programme": {
                "pid": "b01rryzz",
                "title": "Test",
                "image": {
                    "pid": "p01rryzz"
                }
            }
        }
        """;

        var result = _sut.ParseProgrammeJson(json, ProgrammeType.Tv);

        result!.ThumbnailUrl.Should().Contain("p01rryzz");
        result.ThumbnailUrl.Should().Contain("ichef.bbci.co.uk");
    }

    [Fact]
    public void ParseProgrammeJson_WithMissingSynopsis_DefaultsToShortSynopsis()
    {
        var json = """
        {
            "programme": {
                "pid": "b01rryzz",
                "title": "Test",
                "short_synopsis": "Short description only"
            }
        }
        """;

        var result = _sut.ParseProgrammeJson(json, ProgrammeType.Tv);

        result!.Description.Should().Be("Short description only");
    }
}
