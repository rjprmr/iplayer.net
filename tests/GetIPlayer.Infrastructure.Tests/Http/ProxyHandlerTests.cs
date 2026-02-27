using System.Net;
using FluentAssertions;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Infrastructure.Http;

namespace GetIPlayer.Infrastructure.Tests.Http;

public class ProxyHandlerTests
{
    [Fact]
    public void Configure_WhenDisabled_DisablesProxy()
    {
        var handler = new HttpClientHandler();
        var settings = new ProxySettings { Disabled = true };

        ProxyHandler.Configure(handler, settings);

        handler.UseProxy.Should().BeFalse();
    }

    [Fact]
    public void Configure_WithNoUrl_DoesNothing()
    {
        var handler = new HttpClientHandler();
        var settings = new ProxySettings { Url = null };

        ProxyHandler.Configure(handler, settings);

        // Should not throw; default proxy behavior
    }

    [Fact]
    public void Configure_WithValidHttpUrl_SetsProxy()
    {
        var handler = new HttpClientHandler();
        var settings = new ProxySettings { Url = "http://proxy.example.com:8080" };

        ProxyHandler.Configure(handler, settings);

        handler.UseProxy.Should().BeTrue();
        handler.Proxy.Should().NotBeNull();
    }

    [Fact]
    public void Configure_WithValidHttpsUrl_SetsProxy()
    {
        var handler = new HttpClientHandler();
        var settings = new ProxySettings { Url = "https://proxy.example.com:8443" };

        ProxyHandler.Configure(handler, settings);

        handler.UseProxy.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithInvalidScheme_ThrowsArgumentException()
    {
        var handler = new HttpClientHandler();
        var settings = new ProxySettings { Url = "ftp://proxy.example.com" };

        var act = () => ProxyHandler.Configure(handler, settings);

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("scheme");
    }

    [Fact]
    public void Configure_WithInvalidUrl_ThrowsArgumentException()
    {
        var handler = new HttpClientHandler();
        var settings = new ProxySettings { Url = "not a url" };

        var act = () => ProxyHandler.Configure(handler, settings);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Configure_WithNullHandler_ThrowsArgumentNullException()
    {
        var settings = new ProxySettings();

        var act = () => ProxyHandler.Configure(null!, settings);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Configure_WithNullSettings_ThrowsArgumentNullException()
    {
        var handler = new HttpClientHandler();

        var act = () => ProxyHandler.Configure(handler, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
