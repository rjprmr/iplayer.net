using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Infrastructure.Bbc;
using GetIPlayer.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Infrastructure.Tests.Bbc;

/// <summary>
/// Integration tests for BbcProgrammeService search functionality,
/// verifying that the ibl API JSON format is parsed correctly.
/// </summary>
public class BbcProgrammeServiceSearchTests
{
    private readonly Mock<IHttpClientService> _httpClient = new();
    private readonly BbcProgrammeService _sut;

    public BbcProgrammeServiceSearchTests()
    {
        _sut = new BbcProgrammeService(
            _httpClient.Object,
            Mock.Of<ILogger<BbcProgrammeService>>());
    }

    [Fact]
    public async Task SearchAsync_WithIblApiResponse_ParsesResults()
    {
        var iblJson = """
        {
            "search": {
                "page": 1,
                "per_page": 20,
                "count": 2,
                "results": [
                    {
                        "id": "b006m86d",
                        "title": "EastEnders",
                        "synopses": {
                            "small": "Welcome to Walford.",
                            "medium": "Welcome to Walford, East London."
                        },
                        "master_brand": {
                            "id": "bbc_one",
                            "titles": {
                                "small": "BBC One",
                                "medium": "BBC One",
                                "large": "BBC One HD"
                            }
                        },
                        "images": {
                            "type": "image",
                            "standard": "https://ichef.bbci.co.uk/images/ic/{recipe}/p0abc123.jpg"
                        }
                    },
                    {
                        "id": "b007ytlk",
                        "title": "EastEnders: Secrets from the Square",
                        "synopses": {
                            "small": "Stacey Dooley hosts."
                        },
                        "master_brand": {
                            "id": "bbc_one",
                            "titles": {
                                "small": "BBC One"
                            }
                        }
                    }
                ]
            }
        }
        """;

        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(iblJson);

        var result = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        result.Should().NotBeNull();
        result.Programmes.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        var first = result.Programmes[0];
        first.Pid.Should().Be("b006m86d");
        first.Name.Should().Be("EastEnders");
        first.Description.Should().Be("Welcome to Walford, East London.");
        first.Channel.Should().Be("BBC One");
        first.ThumbnailUrl.Should().Contain("480x270");
        first.ThumbnailUrl.Should().Contain("p0abc123");
        first.Type.Should().Be(ProgrammeType.Tv);

        var second = result.Programmes[1];
        second.Pid.Should().Be("b007ytlk");
        second.Name.Should().Be("EastEnders: Secrets from the Square");
    }

    [Fact]
    public async Task SearchAsync_WithIblApiResponse_SetsCorrectWebUrls()
    {
        var iblJson = """
        {
            "search": {
                "results": [
                    {
                        "id": "b006m86d",
                        "title": "EastEnders"
                    }
                ],
                "count": 1
            }
        }
        """;

        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(iblJson);

        var tvResult = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);
        tvResult.Programmes[0].WebUrl.Should().Contain("iplayer/episode/b006m86d");
    }

    [Fact]
    public async Task SearchAsync_WithRadioType_SetsCorrectWebUrl()
    {
        var iblJson = """
        {
            "search": {
                "results": [
                    {
                        "id": "b006qpgr",
                        "title": "The Archers"
                    }
                ],
                "count": 1
            }
        }
        """;

        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(iblJson);

        var result = await _sut.SearchAsync("archers", [ProgrammeType.Radio]);
        result.Programmes[0].WebUrl.Should().Contain("sounds/play/b006qpgr");
    }

    [Fact]
    public async Task SearchAsync_WithEmptyResults_ReturnsEmptyList()
    {
        var iblJson = """
        {
            "search": {
                "results": [],
                "count": 0,
                "page": 1,
                "per_page": 20
            }
        }
        """;

        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(iblJson);

        var result = await _sut.SearchAsync("zzzznonexistent", [ProgrammeType.Tv]);

        result.Programmes.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WithChannelFilter_FiltersResults()
    {
        var iblJson = """
        {
            "search": {
                "results": [
                    {
                        "id": "b006m86d",
                        "title": "EastEnders",
                        "master_brand": { "titles": { "small": "BBC One" } }
                    },
                    {
                        "id": "b007ytlk",
                        "title": "Test Show",
                        "master_brand": { "titles": { "small": "BBC Two" } }
                    }
                ],
                "count": 2
            }
        }
        """;

        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(iblJson);

        var result = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv], "BBC One");

        result.Programmes.Should().HaveCount(1);
        result.Programmes[0].Channel.Should().Be("BBC One");
    }

    [Fact]
    public async Task SearchAsync_WhenHttpFails_ReturnsEmptyResult()
    {
        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        result.Programmes.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WhenResponseIsNotJson_ReturnsEmptyResult()
    {
        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html><body>Not JSON</body></html>");

        var result = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        result.Programmes.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_CallsCorrectIblApiUrl()
    {
        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{ "search": { "results": [], "count": 0 } }""");

        await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        _httpClient.Verify(x => x.GetStringAsync(
            It.Is<string>(url =>
                url.Contains("ibl.api.bbc.co.uk") &&
                url.Contains("q=eastenders") &&
                url.Contains("mediatype=tv")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithIblImageTemplate_ReplacesRecipePlaceholder()
    {
        var iblJson = """
        {
            "search": {
                "results": [
                    {
                        "id": "b006m86d",
                        "title": "Show",
                        "images": {
                            "standard": "https://ichef.bbci.co.uk/images/ic/{recipe}/p0abc123.jpg"
                        }
                    }
                ],
                "count": 1
            }
        }
        """;

        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(iblJson);

        var result = await _sut.SearchAsync("show", [ProgrammeType.Tv]);

        result.Programmes[0].ThumbnailUrl.Should().Be("https://ichef.bbci.co.uk/images/ic/480x270/p0abc123.jpg");
        result.Programmes[0].ThumbnailUrl.Should().NotContain("{recipe}");
    }
}
