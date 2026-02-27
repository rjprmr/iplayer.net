using System.Threading.Channels;
using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Pages;
using GetIPlayer.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace GetIPlayer.Web.Tests.Pages;

public class DownloadModelTests
{
    private readonly DownloadTracker _tracker = new();
    private readonly Channel<DownloadRequest> _downloadChannel;
    private readonly DownloadModel _sut;

    public DownloadModelTests()
    {
        _downloadChannel = Channel.CreateUnbounded<DownloadRequest>();
        _sut = new DownloadModel(_tracker, _downloadChannel)
        {
            PageContext = CreatePageContext(),
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public void OnGet_LoadsTrackerData()
    {
        _tracker.UpdateProgress("dl-1", new DownloadProgress { Pid = "b01rryzz", Phase = "Downloading" });

        _sut.OnGet();

        _sut.ActiveDownloads.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnPostAsync_WithValidTarget_QueuesDownload()
    {
        _sut.Target = "b01rryzz";

        var result = await _sut.OnPostAsync(CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();
        _downloadChannel.Reader.TryRead(out var req).Should().BeTrue();
        req!.Target.Should().Be("b01rryzz");
    }

    [Fact]
    public async Task OnPostAsync_WithEmptyTarget_ReturnsPage()
    {
        _sut.Target = "";

        var result = await _sut.OnPostAsync(CancellationToken.None);

        result.Should().BeOfType<PageResult>();
        _sut.StatusMessage.Should().Contain("Please enter");
    }

    [Fact]
    public async Task OnPostAsync_WithNullTarget_ReturnsPage()
    {
        _sut.Target = null;

        var result = await _sut.OnPostAsync(CancellationToken.None);

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAsync_WithOverlongTarget_ReturnsPage()
    {
        _sut.Target = new string('a', 600);

        var result = await _sut.OnPostAsync(CancellationToken.None);

        result.Should().BeOfType<PageResult>();
        _sut.StatusMessage.Should().Contain("too long");
    }

    [Fact]
    public async Task OnPostAsync_WithForceAndSubtitles_PassesOptions()
    {
        _sut.Target = "b01rryzz";
        _sut.Force = true;
        _sut.Subtitles = true;
        _sut.Overwrite = true;

        await _sut.OnPostAsync(CancellationToken.None);

        _downloadChannel.Reader.TryRead(out var req).Should().BeTrue();
        req!.Force.Should().BeTrue();
        req.Subtitles.Should().BeTrue();
        req.Overwrite.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostAsync_TrimsWhitespace()
    {
        _sut.Target = "  b01rryzz  ";

        await _sut.OnPostAsync(CancellationToken.None);

        _downloadChannel.Reader.TryRead(out var req).Should().BeTrue();
        req!.Target.Should().Be("b01rryzz");
    }

    private static PageContext CreatePageContext()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        return new PageContext(actionContext);
    }
}
