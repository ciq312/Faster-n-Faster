using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Races.RaceEndOrchestration;
using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.Api.UseCases.Realtime;
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
        var scope = LobbyFactory.StateScope(context, broadcaster);

        await scope.Run(async () =>
        {
            await handler.Handle(Notification(context.LobbyId), CancellationToken.None);

            Assert.Empty(broadcaster.Broadcasts);
        });

        Assert.Single(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task NextRaceAlreadyEnrolled_PassageKeptAndNoThrow()
    {
        var (finished, context, hostId) = await FinishedRace();
        var passageBefore = await context.ReadRace(r => r.Passage);
        var provider = new GatedPassageProvider();
        var handling = Record.ExceptionAsync(() => HandlerWith(context, provider)
            .Handle(new DomainEventNotification<RaceFinishedEvent>(finished), CancellationToken.None));
        await provider.WaitForRequest();

        await LobbyFactory.StartRace(context, hostId);
        provider.Complete("fresh words");
        var exception = await handling;

        Assert.Null(exception);
        Assert.Equal(passageBefore, await context.ReadRace(r => r.Passage));
    }

    [Fact]
    public async Task FinishedRace_PassageReplacedForNextRace()
    {
        var (finished, context, _) = await FinishedRace();
        var handler = HandlerWith(context, new FakePassageProvider("fresh words"));

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(finished), CancellationToken.None);

        Assert.Equal("fresh words", await context.ReadRace(r => r.Passage));
    }

    [Fact]
    public async Task StartRaceDuringPostRaceRefresh_KeepsNextRaceRegistered()
    {
        var (finished, context, hostId) = await FinishedRace();
        var provider = new GatedPassageProvider();
        var handling = HandlerWith(context, provider)
            .Handle(new DomainEventNotification<RaceFinishedEvent>(finished), CancellationToken.None);
        await provider.WaitForRequest();

        await LobbyFactory.StartRace(context, hostId);
        provider.Complete("fresh words");
        await handling;

        Assert.Contains(context.Registry.GetRacingLobbies(), e => e.LobbyId == context.LobbyId);
    }

    private static async Task<(RaceFinishedEvent Finished, LobbyTestContext Context, Guid HostId)> FinishedRace()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        foreach (var playerId in new[] { host.Id, other.Id })
            await context.Races.Mutate(context.LobbyId, r => r.WithdrawParticipant(playerId));

        return (Assert.Single(context.Dispatcher.Dispatched.OfType<RaceFinishedEvent>()), context, host.Id);
    }

    private static RaceFinishedOrchestrationHandler HandlerWith(LobbyTestContext context, IPassageProvider provider) =>
        new(
            context.Lobbies,
            new PassageRefresher(context.Lobbies, context.Races, provider, context.Tracker),
            context.Registry,
            LobbyFactory.StateScope(context, new FakeBroadcaster()));

    private static DomainEventNotification<RaceFinishedEvent> Notification(Guid lobbyId) =>
        new(new RaceFinishedEvent(lobbyId, new List<RaceParticipantResult>()));

    private static async Task<(RaceFinishedOrchestrationHandler Handler, LobbyTestContext Context, FakeBroadcaster Broadcaster)> Build()
    {
        var host = new User("host");
        var other = new User("other");
        var context = await LobbyFactory.WithPlayers(host, other);
        await LobbyFactory.StartRace(context, host.Id);

        var broadcaster = new FakeBroadcaster();
        var scope = LobbyFactory.StateScope(context, broadcaster);

        var handler = new RaceFinishedOrchestrationHandler(context.Lobbies, context.PassageRefresher, context.Registry, scope);

        return (handler, context, broadcaster);
    }
}
