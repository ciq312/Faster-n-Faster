using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.StartRace;

public class StartRaceHandler(
    ILobbyStore lobbies,
    IRaceStore races,
    IRaceTickRegistry raceTickRegistry,
    IAntiCheatPolicy antiCheatPolicy) : IRequestHandler<StartRaceCommand, Guid>
{
    public async Task<Guid> Handle(StartRaceCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayerRequired(command.UserId);

        List<RaceParticipant> participants = [];
        await lobbies.Mutate(lobbyId, l =>
        {
            l.StartSession(command.UserId);
            participants = l.GetRaceParticipants(antiCheatPolicy);
        });

        await races.Mutate(lobbyId, r => r.AddParticipants(participants));

        raceTickRegistry.RegisterLobby(lobbyId);

        return lobbyId;
    }
}
