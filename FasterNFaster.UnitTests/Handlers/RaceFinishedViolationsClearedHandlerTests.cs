using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Realtime.AntiCheat;

namespace FasterNFaster.Tests.Handlers;

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
