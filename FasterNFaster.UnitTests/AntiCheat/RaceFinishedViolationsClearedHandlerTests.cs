using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.Infrastructure.AntiCheat;
using FasterNFaster.Api.UseCases.AntiCheat.Sanctions;
using FasterNFaster.Api.UseCases.Shared;

namespace FasterNFaster.UnitTests.AntiCheat;

public class RaceFinishedViolationsClearedHandlerTests
{
    [Fact]
    public async Task Handle_ClearsRaceViolationCountForThatLobby()
    {
        var strikes = new StrikeRegistry();
        var lobbyId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        strikes.RecordRaceViolation(lobbyId, playerId);
        strikes.RecordRaceViolation(lobbyId, playerId);
        var handler = new RaceFinishedViolationsClearedHandler(strikes);

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(
            new RaceFinishedEvent(lobbyId, new List<RaceParticipantResult>())), CancellationToken.None);

        Assert.Equal(1, strikes.RecordRaceViolation(lobbyId, playerId));
    }
}
