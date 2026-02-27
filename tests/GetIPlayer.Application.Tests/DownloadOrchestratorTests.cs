using FluentAssertions;
using GetIPlayer.Application;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Exceptions;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace GetIPlayer.Application.Tests;

public class DownloadOrchestratorTests
{
    private readonly Mock<IProgrammeService> _programmeService = new();
    private readonly Mock<IStreamService> _streamService = new();
    private readonly Mock<IHlsDownloader> _hlsDownloader = new();
    private readonly Mock<IDashDownloader> _dashDownloader = new();
    private readonly Mock<IPostProcessor> _postProcessor = new();
    private readonly Mock<ISubtitleService> _subtitleService = new();
    private readonly Mock<IHistoryService> _historyService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<ILogger<DownloadOrchestrator>> _logger = new();
    private readonly DownloadOptions _downloadOptions;
    private readonly OutputOptions _outputOptions;
    private readonly DownloadOrchestrator _sut;

    public DownloadOrchestratorTests()
    {
        _downloadOptions = new DownloadOptions();
        _outputOptions = new OutputOptions { OutputDir = "/tmp/downloads" };

        _sut = new DownloadOrchestrator(
            _programmeService.Object,
            _streamService.Object,
            _hlsDownloader.Object,
            _dashDownloader.Object,
            _postProcessor.Object,
            _subtitleService.Object,
            _historyService.Object,
            _cacheService.Object,
            _fileSystem.Object,
            _logger.Object,
            Options.Create(_downloadOptions),
            Options.Create(_outputOptions));
    }

    [Fact]
    public async Task DownloadAsync_WhenAlreadyInHistory_ReturnsSkipped()
    {
        _historyService.Setup(x => x.ExistsAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DownloadAsync("b01rryzz");

        result.Status.Should().Be(DownloadStatus.Skipped);
        result.ErrorMessage.Should().Contain("history");
    }

    [Fact]
    public async Task DownloadAsync_WhenForceEnabled_DoesNotCheckHistory()
    {
        var options = new DownloadOptions { Force = true };
        var sut = CreateSut(downloadOptions: options);

        var programme = CreateTestProgramme();
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(programme);

        var streams = new List<StreamInfo> { CreateTestStream() };
        _streamService.Setup(x => x.GetStreamsAsync("b01rryzz", It.IsAny<QualityLevel[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streams);

        _hlsDownloader.Setup(x => x.DownloadAsync(It.IsAny<StreamInfo>(), It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000L);
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        _fileSystem.Setup(x => x.GetFileSize(It.IsAny<string>())).Returns(1000L);

        await sut.DownloadAsync("b01rryzz");

        _historyService.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenProgrammeNotFound_ReturnsFailed()
    {
        _historyService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Programme?)null);

        var result = await _sut.DownloadAsync("b01rryzz");

        result.Status.Should().Be(DownloadStatus.Failed);
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DownloadAsync_WhenNoStreamsAvailable_ReturnsUnavailable()
    {
        _historyService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProgramme());
        _streamService.Setup(x => x.GetStreamsAsync("b01rryzz", It.IsAny<QualityLevel[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StreamInfo>());

        var result = await _sut.DownloadAsync("b01rryzz");

        result.Status.Should().Be(DownloadStatus.Unavailable);
        result.ErrorMessage.Should().Contain("No available streams");
    }

    [Fact]
    public async Task DownloadAsync_WhenFileExistsAndNotOverwrite_ReturnsSkipped()
    {
        _historyService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProgramme());
        _streamService.Setup(x => x.GetStreamsAsync("b01rryzz", It.IsAny<QualityLevel[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StreamInfo> { CreateTestStream() });
        _fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

        var result = await _sut.DownloadAsync("b01rryzz");

        result.Status.Should().Be(DownloadStatus.Skipped);
        result.ErrorMessage.Should().Contain("File already exists");
    }

    [Fact]
    public async Task DownloadAsync_WhenGeoBlocked_ReturnsFailed()
    {
        _historyService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GeoBlockedException("b01rryzz"));

        var result = await _sut.DownloadAsync("b01rryzz");

        result.Status.Should().Be(DownloadStatus.Failed);
        result.ErrorMessage.Should().Contain("geo-blocked");
    }

    [Fact]
    public async Task DownloadAsync_WhenStreamUnavailable_ReturnsUnavailable()
    {
        _historyService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProgramme());
        _streamService.Setup(x => x.GetStreamsAsync("b01rryzz", It.IsAny<QualityLevel[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StreamUnavailableException("b01rryzz"));

        var result = await _sut.DownloadAsync("b01rryzz");

        result.Status.Should().Be(DownloadStatus.Unavailable);
    }

    [Fact]
    public async Task DownloadAsync_WhenCancelled_ReturnsCancelled()
    {
        _historyService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _programmeService.Setup(x => x.GetByPidAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await _sut.DownloadAsync("b01rryzz");

        result.Status.Should().Be(DownloadStatus.Cancelled);
    }

    [Fact]
    public async Task DownloadAsync_WithNullOrWhitespacePid_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.DownloadAsync(""))
            .Should().ThrowAsync<ArgumentException>();

        await _sut.Invoking(s => s.DownloadAsync("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DownloadByIndexAsync_WhenNotFound_ReturnsFailed()
    {
        _cacheService.Setup(x => x.GetByIndexAsync(999, ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Programme?)null);
        _cacheService.Setup(x => x.GetByIndexAsync(999, ProgrammeType.Radio, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Programme?)null);

        var result = await _sut.DownloadByIndexAsync(999);

        result.Status.Should().Be(DownloadStatus.Failed);
        result.ErrorMessage.Should().Contain("index 999");
    }

    [Fact]
    public async Task DownloadByIndexAsync_WhenFound_DownloadsByPid()
    {
        var programme = CreateTestProgramme();
        _cacheService.Setup(x => x.GetByIndexAsync(1, ProgrammeType.Tv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(programme);
        _historyService.Setup(x => x.ExistsAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DownloadByIndexAsync(1);

        result.Pid.Should().Be("b01rryzz");
    }

    [Fact]
    public async Task DownloadByUrlAsync_WithInvalidUrl_ReturnsFailed()
    {
        var result = await _sut.DownloadByUrlAsync("https://example.com/notbbc");

        result.Status.Should().Be(DownloadStatus.Failed);
        result.ErrorMessage.Should().Contain("Could not extract PID");
    }

    [Fact]
    public async Task DownloadByUrlAsync_WithNullOrWhitespace_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.DownloadByUrlAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DownloadByUrlAsync_WithValidUrl_ExtractsPidAndDownloads()
    {
        _historyService.Setup(x => x.ExistsAsync("b01rryzz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DownloadByUrlAsync("https://www.bbc.co.uk/iplayer/episode/b01rryzz");

        result.Pid.Should().Be("b01rryzz");
        result.Status.Should().Be(DownloadStatus.Skipped);
    }

    private DownloadOrchestrator CreateSut(DownloadOptions? downloadOptions = null, OutputOptions? outputOptions = null)
    {
        return new DownloadOrchestrator(
            _programmeService.Object,
            _streamService.Object,
            _hlsDownloader.Object,
            _dashDownloader.Object,
            _postProcessor.Object,
            _subtitleService.Object,
            _historyService.Object,
            _cacheService.Object,
            _fileSystem.Object,
            _logger.Object,
            Options.Create(downloadOptions ?? _downloadOptions),
            Options.Create(outputOptions ?? _outputOptions));
    }

    private static Programme CreateTestProgramme() => new()
    {
        Pid = "b01rryzz",
        Type = ProgrammeType.Tv,
        Name = "Doctor Who",
        Episode = "S01E01",
        Channel = "BBC One"
    };

    private static StreamInfo CreateTestStream() => new()
    {
        Mode = "hvf_hls_hd",
        Protocol = StreamProtocol.Hls,
        Quality = QualityLevel.Hd,
        StreamUrl = new Uri("https://example.com/stream.m3u8"),
        Bitrate = 5000
    };
}
