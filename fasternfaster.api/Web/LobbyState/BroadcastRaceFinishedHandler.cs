using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastRaceFinishedHandler(
    IHubContext<GameHub> hub,
    ILobbyStateBroadcaster broadcaster) : INotificationHandler<RaceSessionEndedEvent>
{
    public async Task Handle(RaceSessionEndedEvent e, CancellationToken cancellationToken)
    {
        await hub.Clients.Group(LobbyGroup(e.Lobby.Id)).SendAsync(Methods.RaceEnded, new RaceEndedDTO(e.Results));

        await broadcaster.BroadcastLobbyState(e.Lobby);
    }
}

public record RaceEndedDTO(IEnumerable<RaceParticipantResult> Results);
