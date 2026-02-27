using System.Threading.Channels;
using FluentAssertions;
using GetIPlayer.Application;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Pages;
using GetIPlayer.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Web.Tests.Pages;

public class SearchModelTests
{
    private readonly Mock<IProgrammeService> _programmeService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly SearchOrchestrator _searchOrchestrator;
    private readonly Channel<DownloadRequest> _downloadChannel;
    private readonly SearchModel _sut;

    public SearchModelTests()
    {
        // Use a real SearchOrchestrator (sealed class) with mocked dependencies
        _programmeService.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<ProgrammeType[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult { Programmes = new List<Programme>().AsReadOnly(), TotalCount = 0, Page = 1, PageSize = 25 });

        _searchOrchestrator = new SearchOrchestrator(
            _programmeService.Object,
            _cacheService.Object,
            Mock.Of<ILogger<SearchOrchestrator>>());

        _downloadChannel = Channel.CreateUnbounded<DownloadRequest>();
        _sut = new SearchModel(_searchOrchestrator, _downloadChannel)
        {
            PageContext = CreatePageContext(),
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGetAsync_WithNoSearchTerm_DoesNotSearch()
    {
        _sut.SearchTerm = null;

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.Results.Should().BeNull();
    }

    [Fact]
    public async Task OnGetAsync_WithEmptySearchTerm_DoesNotSearch()
    {
        _sut.SearchTerm = "";

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.Results.Should().BeNull();
    }

    [Fact]
    public async Task OnGetAsync_WithSearchTerm_PerformsSearch()
    {
        _sut.SearchTerm = "doctor";
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "Doctor Who" }
        };
        _programmeService.Setup(x => x.SearchAsync("doctor", It.IsAny<ProgrammeType[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult { Programmes = programmes.AsReadOnly(), TotalCount = 1, Page = 1, PageSize = 25 });

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.Results.Should().NotBeNull();
        _sut.Results!.Programmes.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnGetAsync_WithTvTypeFilter_SearchesTvOnly()
    {
        _sut.SearchTerm = "test";
        _sut.TypeFilter = "TV";
        var programmes = new List<Programme>
        {
            new() { Pid = "b01rryzz", Type = ProgrammeType.Tv, Name = "test show" }
        };
        _programmeService.Setup(x => x.SearchAsync("test", It.IsAny<ProgrammeType[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult { Programmes = programmes.AsReadOnly(), TotalCount = 1, Page = 1, PageSize = 25 });

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.Results.Should().NotBeNull();
        // Only TV should have been searched (not Radio)
        _programmeService.Verify(x => x.SearchAsync("test", It.IsAny<ProgrammeType[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_TruncatesLongSearchTerms()
    {
        _sut.SearchTerm = new string('a', 500);

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.SearchTerm!.Length.Should().BeLessThanOrEqualTo(200);
    }

    [Fact]
    public async Task OnPostDownloadSelectedAsync_WithNoPids_RedirectsWithMessage()
    {
        var result = await _sut.OnPostDownloadSelectedAsync([], CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();
        _sut.StatusMessage.Should().Contain("No programmes selected");
    }

    [Fact]
    public async Task OnPostDownloadSelectedAsync_WithPids_QueuesDownloads()
    {
        var result = await _sut.OnPostDownloadSelectedAsync(["b01rryzz", "b02rryzz"], CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();

        _downloadChannel.Reader.TryRead(out var req1).Should().BeTrue();
        req1!.Target.Should().Be("b01rryzz");
        _downloadChannel.Reader.TryRead(out var req2).Should().BeTrue();
        req2!.Target.Should().Be("b02rryzz");
    }

    [Fact]
    public async Task OnPostDownloadSelectedAsync_SkipsEmptyPids()
    {
        await _sut.OnPostDownloadSelectedAsync(["b01rryzz", "", "  "], CancellationToken.None);

        _downloadChannel.Reader.TryRead(out var req).Should().BeTrue();
        req!.Target.Should().Be("b01rryzz");
        // Empty/whitespace should be skipped
    }

    private static PageContext CreatePageContext()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        return new PageContext(actionContext);
    }
}
