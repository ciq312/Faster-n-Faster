using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public class RefreshPassageHandler(
    ILobbyAccess lobbies,
    IRaceInternals raceInternals,
    ILobbyQuery lobbyQuery,
    IBroadcaster broadcaster) : IRequestHandler<RefreshPassageCommand>
{
    public async Task Handle(RefreshPassageCommand command, CancellationToken cancellationToken)
    {
        Lobby lobby = lobbies.GetOfPlayerRequired(command.CallerId);

        if (lobby.IsSessionActive) throw new InvalidOperationException("Can't refresh when session active");

        lobby.ValidateHost(command.CallerId);

        await raceInternals.RefreshPassage(lobby.Id);

        await broadcaster.Broadcast(Audience.Lobby(lobby.Id), GameEvents.LobbyState, await lobbyQuery.GetLobbyState(lobby.Id));
    }
}
