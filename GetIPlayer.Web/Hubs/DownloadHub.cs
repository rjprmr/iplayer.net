using Microsoft.AspNetCore.SignalR;

namespace GetIPlayer.Web.Hubs;

/// <summary>
/// SignalR hub for broadcasting download progress to connected web clients.
/// </summary>
public sealed class DownloadHub : Hub
{
    /// <summary>
    /// Subscribe the caller to progress updates for a specific download.
    /// </summary>
    public async Task SubscribeToDownload(string downloadId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, downloadId);
    }

    /// <summary>
    /// Unsubscribe the caller from a specific download's updates.
    /// </summary>
    public async Task UnsubscribeFromDownload(string downloadId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, downloadId);
    }
}
