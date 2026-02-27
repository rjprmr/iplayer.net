using FluentAssertions;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Pages.History;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace GetIPlayer.Web.Tests.Pages;

public class HistoryIndexModelTests
{
    private readonly Mock<IHistoryService> _historyService = new();
    private readonly IndexModel _sut;

    public HistoryIndexModelTests()
    {
        _sut = new IndexModel(_historyService.Object)
        {
            PageContext = CreatePageContext(),
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGetAsync_LoadsAllRecords()
    {
        var records = new List<HistoryRecord>
        {
            new() { Pid = "b01rryzz", Name = "Show A", DownloadedAt = DateTimeOffset.UtcNow },
            new() { Pid = "b02rryzz", Name = "Show B", DownloadedAt = DateTimeOffset.UtcNow.AddHours(-2) }
        };
        _historyService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.Records.Should().HaveCount(2);
        _sut.Records[0].Pid.Should().Be("b01rryzz");
    }

    [Fact]
    public async Task OnGetAsync_OrdersByDownloadedAtDescending()
    {
        var older = DateTimeOffset.UtcNow.AddDays(-1);
        var newer = DateTimeOffset.UtcNow;
        var records = new List<HistoryRecord>
        {
            new() { Pid = "older", Name = "Older", DownloadedAt = older },
            new() { Pid = "newer", Name = "Newer", DownloadedAt = newer }
        };
        _historyService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.Records[0].Pid.Should().Be("newer");
        _sut.Records[1].Pid.Should().Be("older");
    }

    [Fact]
    public async Task OnPostClearAsync_ClearsHistory()
    {
        var result = await _sut.OnPostClearAsync(CancellationToken.None);

        _historyService.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<RedirectToPageResult>();
        _sut.StatusMessage.Should().Contain("cleared");
    }

    [Fact]
    public async Task OnGetAsync_WhenEmpty_ReturnsEmptyList()
    {
        _historyService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HistoryRecord>());

        await _sut.OnGetAsync(CancellationToken.None);

        _sut.Records.Should().BeEmpty();
    }

    private static PageContext CreatePageContext()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        return new PageContext(actionContext);
    }
}
