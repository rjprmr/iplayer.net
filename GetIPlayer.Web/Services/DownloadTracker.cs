using System.Collections.Concurrent;
using GetIPlayer.Core.Enums;
using GetIPlayer.Core.Models;

namespace GetIPlayer.Web.Services;

/// <summary>
/// Tracks active and completed downloads for display in the web UI.
/// </summary>
public sealed class DownloadTracker
{
    private readonly ConcurrentDictionary<string, DownloadProgressInfo> _active = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<DownloadResultInfo> _completed = new();
    private const int MaxCompletedHistory = 50;

    public void UpdateProgress(string downloadId, DownloadProgress progress)
    {
        _active[downloadId] = new DownloadProgressInfo
        {
            DownloadId = downloadId,
            Pid = progress.Pid,
            Phase = progress.Phase,
            PercentComplete = progress.PercentComplete,
            BytesDownloaded = progress.BytesDownloaded,
            TotalBytes = progress.TotalBytes,
            BytesPerSecond = progress.BytesPerSecond,
            EstimatedTimeRemaining = progress.EstimatedTimeRemaining,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkCompleted(string downloadId, DownloadResult result)
    {
        _active.TryRemove(downloadId, out _);
        _completed.Enqueue(new DownloadResultInfo
        {
            DownloadId = downloadId,
            Pid = result.Pid,
            Status = result.Status,
            FilePath = result.FilePath,
            BytesDownloaded = result.BytesDownloaded,
            ErrorMessage = result.ErrorMessage,
            CompletedAt = DateTimeOffset.UtcNow
        });

        // Trim old completed entries
        while (_completed.Count > MaxCompletedHistory)
        {
            _completed.TryDequeue(out _);
        }
    }

    public IReadOnlyList<DownloadProgressInfo> GetActiveDownloads() =>
        _active.Values.OrderByDescending(d => d.UpdatedAt).ToList();

    public IReadOnlyList<DownloadResultInfo> GetRecentCompleted() =>
        _completed.Reverse().Take(20).ToList();

    public sealed class DownloadProgressInfo
    {
        public required string DownloadId { get; init; }
        public required string Pid { get; init; }
        public string Phase { get; init; } = string.Empty;
        public double? PercentComplete { get; init; }
        public long BytesDownloaded { get; init; }
        public long TotalBytes { get; init; }
        public double BytesPerSecond { get; init; }
        public TimeSpan? EstimatedTimeRemaining { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }

    public sealed class DownloadResultInfo
    {
        public required string DownloadId { get; init; }
        public required string Pid { get; init; }
        public DownloadStatus Status { get; init; }
        public string? FilePath { get; init; }
        public long BytesDownloaded { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTimeOffset CompletedAt { get; init; }
    }
}
