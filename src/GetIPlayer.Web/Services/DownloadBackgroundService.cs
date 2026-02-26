using System.Threading.Channels;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Core.Models;
using GetIPlayer.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GetIPlayer.Web.Services;

/// <summary>
/// Background service that processes queued download requests.
/// </summary>
public sealed partial class DownloadBackgroundService : BackgroundService
{
    private readonly Channel<DownloadRequest> _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<DownloadHub> _hubContext;
    private readonly DownloadTracker _tracker;
    private readonly ILogger<DownloadBackgroundService> _logger;

    public DownloadBackgroundService(
        Channel<DownloadRequest> queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<DownloadHub> hubContext,
        DownloadTracker tracker,
        ILogger<DownloadBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _tracker = tracker;
        _logger = logger;
    }

    public ValueTask EnqueueAsync(DownloadRequest request, CancellationToken ct = default) =>
        _queue.Writer.WriteAsync(request, ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted();

        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessDownloadAsync(request, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogDownloadError(request.Target, ex);
            }
        }
    }

    private async Task ProcessDownloadAsync(DownloadRequest request, CancellationToken ct)
    {
        LogDownloadStarted(request.Target, request.Id);

        using var scope = _scopeFactory.CreateScope();
        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();

        var progress = new Progress<DownloadProgress>(async p =>
        {
            _tracker.UpdateProgress(request.Id, p);

            // Broadcast to SignalR group
            await _hubContext.Clients.Group(request.Id).SendAsync("DownloadProgress", new
            {
                request.Id,
                p.Pid,
                p.Phase,
                p.PercentComplete,
                p.BytesDownloaded,
                p.TotalBytes,
                p.BytesPerSecond,
                EstimatedTimeRemaining = p.EstimatedTimeRemaining?.TotalSeconds
            }, ct);
        });

        DownloadResult result;
        if (int.TryParse(request.Target, out var index))
        {
            result = await downloadService.DownloadByIndexAsync(index, progress, ct);
        }
        else if (request.Target.Contains("bbc.co.uk", StringComparison.OrdinalIgnoreCase) ||
                 request.Target.Contains("bbc.com", StringComparison.OrdinalIgnoreCase))
        {
            result = await downloadService.DownloadByUrlAsync(request.Target, progress, ct);
        }
        else
        {
            result = await downloadService.DownloadAsync(request.Target, progress, ct);
        }

        _tracker.MarkCompleted(request.Id, result);

        // Notify completion
        await _hubContext.Clients.Group(request.Id).SendAsync("DownloadComplete", new
        {
            request.Id,
            result.Pid,
            Status = result.Status.ToString(),
            result.FilePath,
            result.BytesDownloaded,
            result.ErrorMessage
        }, ct);

        LogDownloadCompleted(request.Target, result.Status);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Download background service started")]
    private partial void LogServiceStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Download started: {Target} (ID: {DownloadId})")]
    private partial void LogDownloadStarted(string target, string downloadId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Download completed: {Target} -> {Status}")]
    private partial void LogDownloadCompleted(string target, GetIPlayer.Core.Enums.DownloadStatus status);

    [LoggerMessage(Level = LogLevel.Error, Message = "Download failed for {Target}")]
    private partial void LogDownloadError(string target, Exception ex);
}
