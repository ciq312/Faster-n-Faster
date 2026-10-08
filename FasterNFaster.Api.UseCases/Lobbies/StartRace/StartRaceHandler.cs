using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public class StartRaceHandler(
    ILobbyAccess lobbies,
    IRaceAccess races,
    IRaceTickRegistry raceTickRegistry,
    IAntiCheatPolicy antiCheatPolicy) : IRequestHandler<StartRaceCommand, Guid>
{
    public async Task<Guid> Handle(StartRaceCommand command, CancellationToken cancellationToken)
    {
        var lobby = lobbies.GetOfPlayerRequired(command.UserId);
        var lobbyId = lobby.Id;

        await lobbies.Mutate(lobbyId, l =>
        {
            l.ValidateHost(command.UserId);
            l.StartSession();
        });

        await races.Mutate(lobbyId, r => r.AddParticipants(lobby.GetRaceParticipants(antiCheatPolicy)));

        raceTickRegistry.RegisterLobby(lobbyId);

        return lobbyId;
    }
}
