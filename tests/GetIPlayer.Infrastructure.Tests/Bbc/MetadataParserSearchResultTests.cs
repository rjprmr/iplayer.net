using System.Text.Json;
using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Infrastructure.Bbc;

namespace GetIPlayer.Infrastructure.Tests.Bbc;

/// <summary>
/// Tests for MetadataParser.ParseSearchResultElement covering both
/// ibl API format and legacy programmes API format.
/// </summary>
public class MetadataParserSearchResultTests
{
    [Fact]
    public void ParseSearchResultElement_WithIblFormat_ParsesCorrectly()
    {
        var json = """
        {
            "id": "b006m86d",
            "title": "EastEnders",
            "subtitle": "Episode 1",
            "synopses": {
                "small": "Short synopsis",
                "medium": "Medium synopsis"
            },
            "master_brand": {
                "id": "bbc_one",
                "titles": {
                    "small": "BBC One"
                }
            },
            "images": {
                "standard": "https://ichef.bbci.co.uk/images/ic/{recipe}/p0abc123.jpg"
            }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Tv);

        result.Should().NotBeNull();
        result!.Pid.Should().Be("b006m86d");
        result.Name.Should().Be("EastEnders");
        result.Episode.Should().Be("Episode 1");
        result.Description.Should().Be("Medium synopsis");
        result.Channel.Should().Be("BBC One");
        result.ThumbnailUrl.Should().Be("https://ichef.bbci.co.uk/images/ic/480x270/p0abc123.jpg");
        result.WebUrl.Should().Contain("iplayer/episode/b006m86d");
        result.Type.Should().Be(ProgrammeType.Tv);
    }

    [Fact]
    public void ParseSearchResultElement_WithLegacyProgrammesFormat_ParsesCorrectly()
    {
        var json = """
        {
            "pid": "b01rryzz",
            "title": "Doctor Who",
            "display_title": {
                "title": "Doctor Who",
                "subtitle": "The Day of the Doctor"
            },
            "short_synopsis": "Short",
            "medium_synopsis": "Medium description",
            "ownership": {
                "service": {
                    "title": "BBC One"
                }
            },
            "image": {
                "pid": "p01rryzz"
            }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Tv);

        result.Should().NotBeNull();
        result!.Pid.Should().Be("b01rryzz");
        result.Name.Should().Be("Doctor Who");
        result.Episode.Should().Be("The Day of the Doctor");
        result.Description.Should().Be("Medium description");
        result.Channel.Should().Be("BBC One");
        result.ThumbnailUrl.Should().Contain("p01rryzz");
    }

    [Fact]
    public void ParseSearchResultElement_WithNoId_ReturnsNull()
    {
        var json = """{ "title": "No ID programme" }""";

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Tv);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseSearchResultElement_WithRadioType_SetsSoundsUrl()
    {
        var json = """
        {
            "id": "b006qpgr",
            "title": "The Archers"
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Radio);

        result.Should().NotBeNull();
        result!.WebUrl.Should().Contain("sounds/play/b006qpgr");
    }

    [Fact]
    public void ParseSearchResultElement_WithMissingSynopses_FallsBackToShort()
    {
        var json = """
        {
            "id": "b006m86d",
            "title": "Show",
            "synopses": {
                "small": "Short only"
            }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Tv);

        result!.Description.Should().Be("Short only");
    }

    [Fact]
    public void ParseSearchResultElement_WithNoSynopses_ReturnsEmptyDescription()
    {
        var json = """
        {
            "id": "b006m86d",
            "title": "Show"
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Tv);

        result!.Description.Should().BeEmpty();
    }

    [Fact]
    public void ParseSearchResultElement_WithIblImageTemplate_ReplacesRecipe()
    {
        var json = """
        {
            "id": "b006m86d",
            "title": "Show",
            "images": {
                "standard": "https://ichef.bbci.co.uk/images/ic/{recipe}/p0xyz.jpg"
            }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Tv);

        result!.ThumbnailUrl.Should().Be("https://ichef.bbci.co.uk/images/ic/480x270/p0xyz.jpg");
    }

    [Fact]
    public void ParseSearchResultElement_WithLegacyImagePid_BuildsThumbnailUrl()
    {
        var json = """
        {
            "pid": "b01rryzz",
            "title": "Show",
            "image": {
                "pid": "p01rryzz"
            }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = MetadataParser.ParseSearchResultElement(doc.RootElement, ProgrammeType.Tv);

        result!.ThumbnailUrl.Should().Be("https://ichef.bbci.co.uk/images/ic/480x270/p01rryzz.jpg");
    }
}
