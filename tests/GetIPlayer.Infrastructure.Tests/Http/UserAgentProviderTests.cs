using FluentAssertions;
using GetIPlayer.Infrastructure.Http;

namespace GetIPlayer.Infrastructure.Tests.Http;

public class UserAgentProviderTests
{
    [Fact]
    public void GetNext_ReturnsNonEmptyString()
    {
        var provider = new UserAgentProvider();

        var ua = provider.GetNext();

        ua.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetNext_RotatesThroughAgents()
    {
        var provider = new UserAgentProvider();

        var agents = new HashSet<string>();
        for (var i = 0; i < 10; i++)
        {
            agents.Add(provider.GetNext());
        }

        agents.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void GetNext_ReturnsValidUserAgentFormat()
    {
        var provider = new UserAgentProvider();

        var ua = provider.GetNext();

        ua.Should().Contain("Mozilla/5.0");
    }

    [Fact]
    public void GetNext_IsThreadSafe()
    {
        var provider = new UserAgentProvider();
        var agents = new System.Collections.Concurrent.ConcurrentBag<string>();

        Parallel.For(0, 100, _ =>
        {
            agents.Add(provider.GetNext());
        });

        agents.Should().HaveCount(100);
        agents.All(a => !string.IsNullOrEmpty(a)).Should().BeTrue();
    }
}
