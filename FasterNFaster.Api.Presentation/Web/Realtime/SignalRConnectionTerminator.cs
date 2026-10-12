using System.Collections.Concurrent;
using FasterNFaster.Api.Presentation.Web.Realtime.Filters;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Presentation.Web.Realtime;

public class SignalRConnectionTerminator : IConnectionTerminator, IHubFilter
{
    private readonly ConcurrentDictionary<string, TrackedConnection> connections = new();

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var connectionId = context.Context.ConnectionId;
        if (HubUserId.TryGet(context.Context, out var userId))
            connections[connectionId] = new TrackedConnection(userId, context.Context);

        try
        {
            await next(context);
        }
        catch
        {
            // SignalR skips OnDisconnectedAsync when OnConnectedAsync throws.
            connections.TryRemove(connectionId, out _);
            throw;
        }
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        try
        {
            await next(context, exception);
        }
        finally
        {
            connections.TryRemove(context.Context.ConnectionId, out _);
        }
    }

    public void Terminate(Guid userId)
    {
        foreach (var (_, connection) in connections)
        {
            if (connection.UserId == userId)
                connection.Context.Abort();
        }
    }

    private record TrackedConnection(Guid UserId, HubCallerContext Context);
}
