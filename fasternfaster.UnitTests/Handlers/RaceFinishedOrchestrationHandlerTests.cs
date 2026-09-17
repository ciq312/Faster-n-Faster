using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Lobbies.StartRace;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;
using FasterNFaster.Tests;

public class RaceFinishedOrchestrationHandlerTests
{
    [Fact]
    public async Task Handle_ShouldNotThrow()
    {
        User host = new User("host");
        User other = new User("other");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host, other);

        var startRaceHandler = new StartRaceHandler(context.LobbyAccess, context.RaceService, context.Registry);
        await startRaceHandler.Handle(new StartRaceCommand(host.Id), CancellationToken.None);

        var handler = new RaceFinishedOrchestrationHandler(
            context.Store,
            context.LobbySessionService,
            context.Publisher);

        var @event = new RaceFinishedEvent(new List<RaceParticipantResult>());
        @event.WrapRaceContext(context.LobbyId);

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(@event), CancellationToken.None);

        Assert.False(context.Lobby.IsSessionActive);
        Assert.NotNull(context.Store.Get(context.LobbyId));
        Assert.True(context.Publisher.Published.First() is RaceSessionEndedEvent);
    }
}