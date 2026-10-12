using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Races.RaceEndOrchestration;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Races;

public class RaceFinishedOrchestrationHandlerTests
{
    [Fact]
    public async Task Handle_EndsSessionAndKeepsLobby()
    {
        var (handler, context, _) = await Build();

        await handler.Handle(Notification(context.LobbyId), CancellationToken.None);

        Assert.False(await context.ReadLobby(l => l.IsSessionActive));
        Assert.True(context.LobbyExists);
    }

    [Fact]
    public async Task Handle_DeregistersLobbyFromRaceTick()
    {
        var (handler, context, _) = await Build();

        await handler.Handle(Notification(context.LobbyId), CancellationToken.None);

        Assert.Empty(context.Registry.GetRacingLobbies());
    }

    [Fact]
    public async Task Handle_BroadcastsLobbyStateOnce_WhenNoOuterScope()
    {
        var (handler, context, broadcaster) = await Build();

        await handler.Handle(Notification(context.LobbyId), CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.LobbyState, sent.EventName);
    }

    [Fact]
    public async Task Handle_DefersToOuterScope_WhenAlreadyOpen()
    {
        var (handler, context, broadcaster) = await Build();
        var scope = new LobbyStateScope(context.Tracker, context.Lobbies, context.LobbyQuery, broadcaster);

        await scope.Run(async () =>
        {
            await handler.Handle(Notification(context.LobbyId), CancellationToken.None);

            Assert.Empty(broadcaster.Broadcasts);
        });

        Assert.Single(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task Handle_WhenNextRaceAlreadyEnrolled_KeepsPassageAndDoesNotThrow()
    {
        var (handler, context, _) = await Build();
        var passageBefore = await context.ReadRace(r => r.Passage);

        var exception = await Record.ExceptionAsync(() => handler.Handle(Notification(context.LobbyId), CancellationToken.None));

        Assert.Null(exception);
        Assert.Equal(passageBefore, await context.ReadRace(r => r.Passage));
    }

    private static DomainEventNotification<RaceFinishedEvent> Notification(Guid lobbyId) =>
        new(new RaceFinishedEvent(lobbyId, new List<RaceParticipantResult>()));

    private static async Task<(RaceFinishedOrchestrationHandler Handler, LobbyTestContext Context, FakeBroadcaster Broadcaster)> Build()
    {
        var host = new User("host");
        var other = new User("other");
        var context = await LobbyFactory.WithPlayers(host, other);
        await LobbyFactory.StartRace(context, host.Id);

        var broadcaster = new FakeBroadcaster();
        var scope = new LobbyStateScope(context.Tracker, context.Lobbies, context.LobbyQuery, broadcaster);

        var handler = new RaceFinishedOrchestrationHandler(context.Lobbies, context.Tracker, context.PassageRefresher, context.Registry, scope);

        return (handler, context, broadcaster);
    }
}
