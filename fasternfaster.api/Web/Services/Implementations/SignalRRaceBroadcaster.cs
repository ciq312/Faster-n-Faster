using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class SignalRRaceBroadcaster(
    IHubContext<GameHub> hub,
    IBroadcaster broadcaster,
    ISessionService sessionService,
    ILogger<SignalRRaceBroadcaster> logger) : IRaceBroadcaster
{
    private const int PerClientSendTimeoutMs = 150;

    public Task BroadcastRaceStarted(Guid lobbyId) =>
        broadcaster.Broadcast(Audience.Lobby(lobbyId), Methods.RaceStarted);

    public async Task BroadcastRaceState(IEnumerable<Guid> playerIds, IReadOnlyList<ParticipantSnapshot> snapshot) =>
        await hub.Clients.Clients(
            playerIds.Select(sessionService.GetActiveSession).Where(id => id is not null)!)
            .SendAsync(Methods.RaceState, snapshot, CancellationToken.None);

    private async Task SendToPlayerWithTimeout(Guid userId, object payload)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        if (connectionId is null) return;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(PerClientSendTimeoutMs));
        try
        {
            await hub.Clients.Client(connectionId).SendAsync(Methods.RaceState, payload, cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Send to user {UserId} timed out", userId);
        }
    }
}
