using FluentAssertions;
using GetIPlayer.Core.Interfaces;

namespace GetIPlayer.Core.Tests.Models;

public class ProcessResultTests
{
    [Fact]
    public void ProcessResult_Success_WhenExitCodeIsZero()
    {
        var result = new ProcessResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = ""
        };

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void ProcessResult_NotSuccess_WhenExitCodeIsNonZero()
    {
        var result = new ProcessResult
        {
            ExitCode = 1,
            StandardOutput = "",
            StandardError = "error"
        };

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void ProcessResult_Success_NegativeExitCode_ReturnsFalse()
    {
        var result = new ProcessResult
        {
            ExitCode = -1,
            StandardOutput = "",
            StandardError = ""
        };

        result.Success.Should().BeFalse();
    }
}
