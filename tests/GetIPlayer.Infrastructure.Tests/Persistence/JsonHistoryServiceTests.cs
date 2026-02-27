using System.Text.Json;
using FluentAssertions;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;

namespace GetIPlayer.Infrastructure.Tests.Persistence;

public class JsonHistoryServiceTests : IDisposable
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<ILogger<JsonHistoryService>> _logger = new();
    private readonly string _historyPath = "/tmp/test/history.json";
    private readonly JsonHistoryService _sut;

    public JsonHistoryServiceTests()
    {
        _sut = new JsonHistoryService(_fileSystem.Object, _logger.Object, _historyPath);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public async Task AddAsync_WithNewRecord_AddsToHistory()
    {
        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(false);
        string? savedJson = null;
        _fileSystem.Setup(x => x.WriteAllTextAsync(_historyPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, json, _) => savedJson = json)
            .Returns(Task.CompletedTask);

        var record = new HistoryRecord { Pid = "b01rryzz", Name = "Test" };

        await _sut.AddAsync(record);

        savedJson.Should().NotBeNull();
        savedJson.Should().Contain("b01rryzz");
    }

    [Fact]
    public async Task AddAsync_WithExistingPid_UpdatesRecord()
    {
        var existingRecords = new List<HistoryRecord>
        {
            new() { Pid = "b01rryzz", Name = "Old Name" }
        };
        var existingJson = JsonSerializer.Serialize(existingRecords, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(_historyPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingJson);
        string? savedJson = null;
        _fileSystem.Setup(x => x.WriteAllTextAsync(_historyPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, json, _) => savedJson = json)
            .Returns(Task.CompletedTask);

        var newRecord = new HistoryRecord { Pid = "b01rryzz", Name = "New Name" };
        await _sut.AddAsync(newRecord);

        savedJson.Should().NotBeNull();
        savedJson.Should().Contain("New Name");
        // Should still only have one record for this PID
        var deserialized = JsonSerializer.Deserialize<List<HistoryRecord>>(savedJson!, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        deserialized.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExistsAsync_WhenPidInHistory_ReturnsTrue()
    {
        var records = new List<HistoryRecord>
        {
            new() { Pid = "b01rryzz", Name = "Test" }
        };
        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(_historyPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        var result = await _sut.ExistsAsync("b01rryzz");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenPidNotInHistory_ReturnsFalse()
    {
        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(false);

        var result = await _sut.ExistsAsync("b01rryzz");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WhenFileDoesNotExist_ReturnsEmpty()
    {
        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(false);

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithCorruptFile_ReturnsEmpty()
    {
        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(_historyPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync("not valid json");

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByPidAsync_WhenFound_ReturnsRecord()
    {
        var records = new List<HistoryRecord>
        {
            new() { Pid = "b01rryzz", Name = "Doctor Who" },
            new() { Pid = "b02rryzz", Name = "EastEnders" }
        };
        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(true);
        _fileSystem.Setup(x => x.ReadAllTextAsync(_historyPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        var result = await _sut.GetByPidAsync("b01rryzz");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Doctor Who");
    }

    [Fact]
    public async Task GetByPidAsync_WhenNotFound_ReturnsNull()
    {
        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(false);

        var result = await _sut.GetByPidAsync("notfound");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ClearAsync_WhenFileExists_DeletesFile()
    {
        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(true);

        await _sut.ClearAsync();

        _fileSystem.Verify(x => x.DeleteFile(_historyPath), Times.Once);
    }

    [Fact]
    public async Task ClearAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        _fileSystem.Setup(x => x.FileExists(_historyPath)).Returns(false);

        await _sut.Invoking(s => s.ClearAsync()).Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddAsync_WithNull_ThrowsArgumentNullException()
    {
        await _sut.Invoking(s => s.AddAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyPid_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.ExistsAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonHistoryService(null!, _logger.Object, _historyPath));
        Assert.Throws<ArgumentNullException>(() => new JsonHistoryService(_fileSystem.Object, null!, _historyPath));
        Assert.Throws<ArgumentNullException>(() => new JsonHistoryService(_fileSystem.Object, _logger.Object, null!));
    }
}
