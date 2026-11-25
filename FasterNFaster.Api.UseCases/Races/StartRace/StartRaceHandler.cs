using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.StartRace;

public class StartRaceHandler(
    ILobbyStore lobbies,
    IRaceStore races,
    IRaceTickRegistry raceTickRegistry,
    IAntiCheatPolicy antiCheatPolicy,
    ILogger<StartRaceHandler> logger) : IRequestHandler<StartRaceCommand, Guid>
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
        var raceId = await races.TryRead(lobbyId, r => r.Id);

        raceTickRegistry.RegisterLobby(lobbyId);

        logger.LogInformation("Race countdown started for race {RaceId} in lobby {LobbyId} {TelemetryOperation}", raceId, lobbyId, TelemetryOperations.RaceCountdownStarted);
        return lobbyId;
    }
}
