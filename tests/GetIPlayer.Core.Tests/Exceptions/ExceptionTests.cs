using FluentAssertions;
using GetIPlayer.Core.Exceptions;

namespace GetIPlayer.Core.Tests.Exceptions;

public class GetIPlayerExceptionTests
{
    [Fact]
    public void GetIPlayerException_DefaultConstructor_CreatesException()
    {
        var ex = new GetIPlayerException();
        ex.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetIPlayerException_WithMessage_SetsMessage()
    {
        var ex = new GetIPlayerException("test error");
        ex.Message.Should().Be("test error");
    }

    [Fact]
    public void GetIPlayerException_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new GetIPlayerException("outer", inner);
        ex.InnerException.Should().Be(inner);
        ex.Message.Should().Be("outer");
    }
}

public class ProgrammeNotFoundExceptionTests
{
    [Fact]
    public void ProgrammeNotFoundException_SetsPid()
    {
        var ex = new ProgrammeNotFoundException("b01rryzz");
        ex.Pid.Should().Be("b01rryzz");
        ex.Message.Should().Contain("b01rryzz");
    }

    [Fact]
    public void ProgrammeNotFoundException_WithMessage_SetsMessageAndPid()
    {
        var ex = new ProgrammeNotFoundException("b01rryzz", "custom message");
        ex.Pid.Should().Be("b01rryzz");
        ex.Message.Should().Be("custom message");
    }

    [Fact]
    public void ProgrammeNotFoundException_WithInnerException_SetsAll()
    {
        var inner = new Exception("inner");
        var ex = new ProgrammeNotFoundException("b01rryzz", "msg", inner);
        ex.Pid.Should().Be("b01rryzz");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ProgrammeNotFoundException_IsGetIPlayerException()
    {
        var ex = new ProgrammeNotFoundException("b01rryzz");
        ex.Should().BeAssignableTo<GetIPlayerException>();
    }
}

public class GeoBlockedExceptionTests
{
    [Fact]
    public void GeoBlockedException_SetsPid()
    {
        var ex = new GeoBlockedException("b01rryzz");
        ex.Pid.Should().Be("b01rryzz");
        ex.Message.Should().Contain("geo-blocked");
    }

    [Fact]
    public void GeoBlockedException_WithMessage_SetsMessageAndPid()
    {
        var ex = new GeoBlockedException("b01rryzz", "blocked");
        ex.Pid.Should().Be("b01rryzz");
        ex.Message.Should().Be("blocked");
    }

    [Fact]
    public void GeoBlockedException_IsGetIPlayerException()
    {
        var ex = new GeoBlockedException("b01rryzz");
        ex.Should().BeAssignableTo<GetIPlayerException>();
    }
}

public class StreamUnavailableExceptionTests
{
    [Fact]
    public void StreamUnavailableException_SetsPid()
    {
        var ex = new StreamUnavailableException("b01rryzz");
        ex.Pid.Should().Be("b01rryzz");
        ex.Message.Should().Contain("b01rryzz");
    }

    [Fact]
    public void StreamUnavailableException_IsGetIPlayerException()
    {
        var ex = new StreamUnavailableException("b01rryzz");
        ex.Should().BeAssignableTo<GetIPlayerException>();
    }
}

public class DownloadFailedExceptionTests
{
    [Fact]
    public void DownloadFailedException_SetsPid()
    {
        var ex = new DownloadFailedException("b01rryzz");
        ex.Pid.Should().Be("b01rryzz");
        ex.Message.Should().Contain("b01rryzz");
    }

    [Fact]
    public void DownloadFailedException_WithMessage_SetsMessageAndPid()
    {
        var ex = new DownloadFailedException("b01rryzz", "segment failed");
        ex.Pid.Should().Be("b01rryzz");
        ex.Message.Should().Be("segment failed");
    }

    [Fact]
    public void DownloadFailedException_IsGetIPlayerException()
    {
        var ex = new DownloadFailedException("b01rryzz");
        ex.Should().BeAssignableTo<GetIPlayerException>();
    }
}

public class ValidationExceptionTests
{
    [Fact]
    public void ValidationException_WithMessage_SetsMessage()
    {
        var ex = new ValidationException("invalid input");
        ex.Message.Should().Be("invalid input");
        ex.FieldName.Should().BeNull();
    }

    [Fact]
    public void ValidationException_WithFieldName_SetsBoth()
    {
        var ex = new ValidationException("pid", "Invalid PID format");
        ex.FieldName.Should().Be("pid");
        ex.Message.Should().Be("Invalid PID format");
    }

    [Fact]
    public void ValidationException_WithInnerException_SetsAll()
    {
        var inner = new FormatException("bad format");
        var ex = new ValidationException("outer", inner);
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ValidationException_IsGetIPlayerException()
    {
        var ex = new ValidationException("test");
        ex.Should().BeAssignableTo<GetIPlayerException>();
    }
}
