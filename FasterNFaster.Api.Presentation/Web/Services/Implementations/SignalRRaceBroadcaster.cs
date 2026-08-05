using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class SignalRRaceBroadcaster(
    IHubContext<GameHub> hub,
    IBroadcaster broadcaster,
    ISessionService sessionService) : IRaceBroadcaster
{
    public Task BroadcastRaceStarted(Guid lobbyId) =>
        broadcaster.Broadcast(Audience.Lobby(lobbyId), Methods.RaceStarted);

    public async Task BroadcastRaceState(IEnumerable<Guid> playerIds, IReadOnlyList<ParticipantSnapshot> snapshot) =>
        await hub.Clients.Clients(
            playerIds.Select(sessionService.GetActiveSession).Where(id => id is not null)!)
            .SendAsync(Methods.RaceState, snapshot, CancellationToken.None);

}
