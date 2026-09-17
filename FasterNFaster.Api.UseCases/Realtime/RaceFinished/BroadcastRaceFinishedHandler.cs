using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.RaceFinished;

public class BroadcastRaceFinishedHandler(
    IBroadcaster broadcaster,
    ILobbyQuery lobbyQuery) : INotificationHandler<RaceSessionEndedEvent>
{
    public async Task Handle(RaceSessionEndedEvent e, CancellationToken cancellationToken)
    {
        await broadcaster.Broadcast(Audience.Lobby(e.Lobby.Id), GameEvents.RaceEnded, new RaceEndedDTO(e.Results));
        await broadcaster.Broadcast(Audience.Lobby(e.Lobby.Id), GameEvents.LobbyState, await lobbyQuery.GetLobbyState(e.Lobby.Id));
    }
}
