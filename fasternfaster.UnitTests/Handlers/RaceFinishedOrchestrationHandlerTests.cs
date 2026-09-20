using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Lobbies.StartRace;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Tests;
using FasterNFaster.Tests.Fakes;

public class RaceFinishedOrchestrationHandlerTests
{
    [Fact]
    public async Task Handle_EndsSessionAndPublishes()
    {
        var (handler, context, _) = await Build();

        var @event = new RaceFinishedEvent(context.Lobby.Id, new List<RaceParticipantResult>());

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(@event), CancellationToken.None);

        Assert.False(context.Lobby.IsSessionActive);
        Assert.NotNull(context.Store.Get(context.LobbyId));
        Assert.True(context.Publisher.Published.First() is RaceSessionEndedEvent);
    }

    [Fact]
    public async Task Handle_BroadcastsLobbyStateOnce_WhenNoOuterScope()
    {
        var (handler, context, broadcaster) = await Build();

        var @event = new RaceFinishedEvent(context.Lobby.Id, new List<RaceParticipantResult>());

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(@event), CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.LobbyState, sent.EventName);
    }

    [Fact]
    public async Task Handle_DefersToOuterScope_WhenAlreadyOpen()
    {
        var (handler, context, broadcaster) = await Build();
        var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, broadcaster);

        var @event = new RaceFinishedEvent(context.Lobby.Id, new List<RaceParticipantResult>());

        await scope.Run(async () =>
        {
            await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(@event), CancellationToken.None);

            Assert.Empty(broadcaster.Broadcasts);
        });

        Assert.Single(broadcaster.Broadcasts);
    }

    private static async Task<(RaceFinishedOrchestrationHandler Handler, LobbyTestContext Context, FakeBroadcaster Broadcaster)> Build()
    {
        User host = new User("host");
        User other = new User("other");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host, other);

        var startRaceHandler = new StartRaceHandler(context.LobbyAccess, context.RaceAccess, context.Registry);
        await startRaceHandler.Handle(new StartRaceCommand(host.Id), CancellationToken.None);

        var broadcaster = new FakeBroadcaster();
        var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, broadcaster);

        var handler = new RaceFinishedOrchestrationHandler(
            context.LobbyAccess,
            context.RaceAccess,
            scope,
            context.Publisher);

        return (handler, context, broadcaster);
    }
}
