using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Tests;
using FasterNFaster.Tests.Fakes;

public class RaceFinishedOrchestrationHandlerTests
{
    [Fact]
    public async Task Handle_EndsSessionAndKeepsLobby()
    {
        var (handler, context, _) = await Build();

        await handler.Handle(Notification(context.Lobby.Id), CancellationToken.None);

        Assert.False(context.Lobby.IsSessionActive);
        Assert.NotNull(context.Store.Get(context.LobbyId));
    }

    [Fact]
    public async Task Handle_DeregistersLobbyFromRaceTick()
    {
        var (handler, context, _) = await Build();

        await handler.Handle(Notification(context.Lobby.Id), CancellationToken.None);

        Assert.Empty(context.Registry.GetRacingLobbies());
    }

    [Fact]
    public async Task Handle_BroadcastsLobbyStateOnce_WhenNoOuterScope()
    {
        var (handler, context, broadcaster) = await Build();

        await handler.Handle(Notification(context.Lobby.Id), CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.LobbyState, sent.EventName);
    }

    [Fact]
    public async Task Handle_DefersToOuterScope_WhenAlreadyOpen()
    {
        var (handler, context, broadcaster) = await Build();
        var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, broadcaster);

        await scope.Run(async () =>
        {
            await handler.Handle(Notification(context.Lobby.Id), CancellationToken.None);

            Assert.Empty(broadcaster.Broadcasts);
        });

        Assert.Single(broadcaster.Broadcasts);
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
        var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, broadcaster);

        var handler = new RaceFinishedOrchestrationHandler(context.LobbyAccess, context.RaceAccess, context.Registry, scope);

        return (handler, context, broadcaster);
    }
}
