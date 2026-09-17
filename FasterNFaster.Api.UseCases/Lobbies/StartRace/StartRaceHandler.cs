using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler(
    ILobbyAccess lobbies,
    IRaceInternals raceInternals,
    IRaceTickRegistry raceTickRegistry) : IRequestHandler<StartRaceCommand, Guid>
{
    public async Task<Guid> Handle(StartRaceCommand command, CancellationToken cancellationToken)
    {
        Lobby lobby = lobbies.GetOfPlayerRequired(command.UserId);
        Guid lobbyId = lobby.Id;

        await lobbies.Mutate(lobbyId, l =>
        {
            l.ValidateHost(command.UserId);
            l.StartSession();
        });

        await raceInternals.AddParticipants(lobbyId, lobby.GetRaceParticipants());

        raceTickRegistry.RegisterLobby(lobbyId);

        return lobbyId;
    }
}
