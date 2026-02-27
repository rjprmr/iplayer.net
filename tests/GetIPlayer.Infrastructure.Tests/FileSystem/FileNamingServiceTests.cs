using FluentAssertions;
using GetIPlayer.Infrastructure.FileSystem;

namespace GetIPlayer.Infrastructure.Tests.FileSystem;

public class FileNamingServiceTests
{
    [Fact]
    public void GenerateFileName_WithSubstitutions_ReplacesPlaceholders()
    {
        var substitutions = new Dictionary<string, string>
        {
            ["name"] = "Doctor Who",
            ["episode"] = "S01E01",
            ["pid"] = "b01rryzz"
        };

        var result = FileNamingService.GenerateFileName("{name} - {episode} {pid}", substitutions);

        result.Should().Contain("Doctor Who");
        result.Should().Contain("S01E01");
        result.Should().Contain("b01rryzz");
    }

    [Fact]
    public void GenerateFileName_WithUnresolvedPlaceholders_RemovesThem()
    {
        var substitutions = new Dictionary<string, string>
        {
            ["name"] = "Doctor Who"
        };

        var result = FileNamingService.GenerateFileName("{name} - {episode} {pid}", substitutions);

        result.Should().NotContain("{episode}");
        result.Should().NotContain("{pid}");
    }

    [Fact]
    public void GenerateFileName_WithMaxLength_TruncatesResult()
    {
        var substitutions = new Dictionary<string, string>
        {
            ["name"] = "A Very Long Programme Name That Exceeds Maximum"
        };

        var result = FileNamingService.GenerateFileName("{name}", substitutions, maxLength: 20);

        result.Length.Should().BeLessThanOrEqualTo(20);
    }

    [Fact]
    public void GenerateFileName_WithNullTemplate_ThrowsArgumentException()
    {
        var act = () => FileNamingService.GenerateFileName(null!, new Dictionary<string, string>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SanitiseComponent_WithInvalidChars_ReplacesWithUnderscore()
    {
        var result = FileNamingService.SanitiseComponent("file/name:test");

        result.Should().NotContain("/");
        result.Should().NotContain(":");
    }

    [Fact]
    public void SanitiseComponent_WithEmptyInput_ReturnsEmpty()
    {
        FileNamingService.SanitiseComponent("").Should().BeEmpty();
        FileNamingService.SanitiseComponent("   ").Should().BeEmpty();
    }

    [Fact]
    public void SanitiseComponent_WithNormalText_ReturnsSameText()
    {
        var result = FileNamingService.SanitiseComponent("Doctor Who S01E01");
        result.Should().Be("Doctor Who S01E01");
    }

    [Fact]
    public void GenerateFileName_CollapsesMultipleSpaces()
    {
        var substitutions = new Dictionary<string, string>
        {
            ["name"] = "Doctor Who",
            ["episode"] = ""
        };

        var result = FileNamingService.GenerateFileName("{name} - {episode} test", substitutions);

        result.Should().NotContain("  ");
    }
}
