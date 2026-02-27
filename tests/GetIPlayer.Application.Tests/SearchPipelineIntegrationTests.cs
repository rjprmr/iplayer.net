using FluentAssertions;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Infrastructure.Bbc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Application.Tests;

/// <summary>
/// Integration tests for the full search pipeline:
/// SearchOrchestrator → IProgrammeService (BbcProgrammeService) → MetadataParser.
/// Uses a real BbcProgrammeService with mocked HTTP to verify end-to-end parsing.
/// </summary>
public class SearchPipelineIntegrationTests
{
    private readonly Mock<IHttpClientService> _httpClient = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly SearchOrchestrator _sut;

    public SearchPipelineIntegrationTests()
    {
        // Wire up real BbcProgrammeService with mocked HTTP
        var programmeService = new BbcProgrammeService(
            _httpClient.Object,
            Mock.Of<ILogger<BbcProgrammeService>>());

        _sut = new SearchOrchestrator(
            programmeService,
            _cacheService.Object,
            Mock.Of<ILogger<SearchOrchestrator>>());
    }

    [Fact]
    public async Task SearchAsync_EndToEnd_WithIblResponse_ReturnsParsedProgrammes()
    {
        var iblJson = """
        {
            "search": {
                "page": 1,
                "per_page": 20,
                "count": 3,
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
                            "titles": { "small": "BBC One" }
                        },
                        "images": {
                            "standard": "https://ichef.bbci.co.uk/images/ic/{recipe}/p0abc.jpg"
                        }
                    },
                    {
                        "id": "m000tqsy",
                        "title": "EastEnders: Secrets from the Square",
                        "synopses": {
                            "small": "Stacey Dooley hosts.",
                            "medium": "Stacey Dooley hosts a look behind the scenes."
                        },
                        "master_brand": {
                            "id": "bbc_one",
                            "titles": { "small": "BBC One" }
                        }
                    },
                    {
                        "id": "b09qxmml",
                        "title": "EastEnders Extras",
                        "synopses": {
                            "small": "Extra content."
                        },
                        "master_brand": {
                            "id": "bbc_three",
                            "titles": { "small": "BBC Three" }
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
        result.Programmes.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);

        result.Programmes[0].Pid.Should().Be("b006m86d");
        result.Programmes[0].Name.Should().Be("EastEnders");
        result.Programmes[0].Description.Should().Be("Welcome to Walford, East London.");
        result.Programmes[0].Channel.Should().Be("BBC One");
        result.Programmes[0].ThumbnailUrl.Should().Contain("480x270");
        result.Programmes[0].WebUrl.Should().Contain("iplayer/episode/b006m86d");

        result.Programmes[1].Pid.Should().Be("m000tqsy");
        result.Programmes[2].Pid.Should().Be("b09qxmml");
    }

    [Fact]
    public async Task SearchAsync_EndToEnd_SearchesBothTvAndRadio()
    {
        var tvJson = """
        {
            "search": {
                "results": [
                    { "id": "b006m86d", "title": "EastEnders" }
                ],
                "count": 1
            }
        }
        """;
        var radioJson = """
        {
            "search": {
                "results": [],
                "count": 0
            }
        }
        """;

        _httpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvJson)
            .ReturnsAsync(radioJson);

        var result = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv, ProgrammeType.Radio]);

        result.Programmes.Should().HaveCount(1);
        result.Programmes[0].Pid.Should().Be("b006m86d");
    }

    [Fact]
    public async Task SearchAsync_EndToEnd_WhenApiReturnsHtml_ReturnsEmpty()
    {
        // Simulates the old bug where the wrong URL returned HTML instead of JSON
        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html><head><title>BBC iPlayer Search</title></head><body>HTML content</body></html>");

        var result = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        result.Programmes.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_EndToEnd_WhenApiDown_ReturnsEmpty()
    {
        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var result = await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        result.Programmes.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EndToEnd_VerifiesCorrectApiUrlIsCalled()
    {
        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{ "search": { "results": [], "count": 0 } }""");

        await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        _httpClient.Verify(x => x.GetStringAsync(
            It.Is<string>(url =>
                url.StartsWith("https://ibl.api.bbc.co.uk/ibl/v1/search", StringComparison.Ordinal) &&
                url.Contains("q=eastenders") &&
                url.Contains("mediatype=tv")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_EndToEnd_DoesNotTouchCacheForDirectSearch()
    {
        _httpClient.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{ "search": { "results": [], "count": 0 } }""");

        await _sut.SearchAsync("eastenders", [ProgrammeType.Tv]);

        _cacheService.Verify(x => x.IsStaleAsync(It.IsAny<ProgrammeType>()), Times.Never);
        _cacheService.Verify(x => x.RefreshAsync(It.IsAny<ProgrammeType>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheService.Verify(x => x.GetAllAsync(It.IsAny<ProgrammeType>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
