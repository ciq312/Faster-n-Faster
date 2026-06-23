using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class SignalRRaceBroadcaster(
    IHubContext<GameHub> hub,
    ISessionService sessionService,
    ILogger<SignalRRaceBroadcaster> logger) : IRaceBroadcaster
{
    private const int PerClientSendTimeoutMs = 150;

    public Task BroadcastRaceStarted(Guid lobbyId) =>
        hub.Clients.Group(LobbyGroup(lobbyId)).SendAsync(Methods.RaceStarted);

    public Task BroadcastRaceState(IEnumerable<Guid> playerIds, IReadOnlyList<ParticipantSnapshot> snapshot) =>
        Task.WhenAll(playerIds.Select(id => SendToPlayerWithTimeout(id, snapshot)));

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
