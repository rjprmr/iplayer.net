using FluentAssertions;
using GetIPlayer.Core.Exceptions;
using GetIPlayer.Infrastructure.Bbc;

namespace GetIPlayer.Infrastructure.Tests.Bbc;

public class PidValidatorTests
{
    [Theory]
    [InlineData("b01rryzz")]
    [InlineData("b09b5mzk")]
    [InlineData("p0c1rvxz")]
    [InlineData("m001ry4z")]
    [InlineData("b01234567890")]
    public void IsValid_WithValidPids_ReturnsTrue(string pid)
    {
        PidValidator.IsValid(pid).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("b01rry")]
    [InlineData("AAABBBCC")]
    [InlineData("b01rryz!")]
    [InlineData("abcdefgh")]
    public void IsValid_WithInvalidPids_ReturnsFalse(string? pid)
    {
        PidValidator.IsValid(pid).Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_WithValidPid_DoesNotThrow()
    {
        var act = () => PidValidator.EnsureValid("b01rryzz");
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithInvalidPid_ThrowsValidationException()
    {
        var act = () => PidValidator.EnsureValid("invalid");
        act.Should().Throw<ValidationException>()
            .Which.FieldName.Should().Be("pid");
    }

    [Fact]
    public void EnsureValid_WithNull_ThrowsValidationException()
    {
        var act = () => PidValidator.EnsureValid(null);
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData("https://www.bbc.co.uk/iplayer/episode/b01rryzz", "b01rryzz")]
    [InlineData("https://www.bbc.co.uk/iplayer/episode/b01rryzz/some-title", "b01rryzz")]
    [InlineData("https://www.bbc.co.uk/sounds/play/m001ry4z", "m001ry4z")]
    [InlineData("https://www.bbc.com/iplayer/episode/b01rryzz", "b01rryzz")]
    public void ExtractFromUrl_WithValidUrls_ExtractsPid(string url, string expectedPid)
    {
        PidValidator.ExtractFromUrl(url).Should().Be(expectedPid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com")]
    [InlineData("https://www.bbc.co.uk/news")]
    [InlineData("not a url")]
    public void ExtractFromUrl_WithInvalidUrls_ReturnsNull(string? url)
    {
        PidValidator.ExtractFromUrl(url).Should().BeNull();
    }
}
