using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Races;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Realtime.RaceState;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FasterNFaster.UnitTests.Races;

public class RaceTickServiceTests
{
    private static readonly TimeSpan CountdownEnd = RaceCountdown.Duration + RaceCountdown.StartDelay;

    [Fact]
    public async Task CountdownBeforeStartDelayEnds_KeepsRaceWaiting()
    {
        var (context, broadcaster, _, tick) = await StartedRace();
        context.Time.Advance(CountdownEnd - TimeSpan.FromMilliseconds(1));

        await tick.Tick();

        Assert.False(await HasStarted(context));
        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task CountdownAtStartDelayEnd_StartsRace()
    {
        var (context, _, _, tick) = await StartedRace();
        context.Time.Advance(CountdownEnd);

        await tick.Tick();

        Assert.True(await HasStarted(context));
    }

    [Fact]
    public async Task CountdownAtStartDelayEnd_BroadcastsRaceStartedToLobby()
    {
        var (context, broadcaster, _, tick) = await StartedRace();
        context.Time.Advance(CountdownEnd);

        await tick.Tick();

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(nameof(IGameClient.RaceStarted), sent.EventName);
        Assert.Equal(new LobbyAudience(context.LobbyId), sent.Audience);
    }

    [Fact]
    public async Task CountdownAtStartDelayEnd_TransitionsToRacing()
    {
        var (context, _, _, tick) = await StartedRace();
        context.Time.Advance(CountdownEnd);

        await tick.Tick();

        var entry = Assert.Single(context.Registry.GetRacingLobbies());
        Assert.Equal(RacePhase.Racing, entry.Phase);
    }

    [Fact]
    public async Task Racing_BroadcastsRaceStateToConnectedPlayers()
    {
        var (context, broadcaster, _, tick) = await RacingRace();

        await tick.Tick();

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(nameof(IGameClient.RaceState), sent.EventName);
        var audience = Assert.IsType<PlayersAudience>(sent.Audience);
        Assert.Equal((await context.ReadLobby(l => l.GetPlayerIds().ToList())).ToHashSet(), audience.PlayerIds.ToHashSet());
        var snapshot = Assert.IsAssignableFrom<IReadOnlyList<RaceParticipantDTO>>(sent.Payload);
        Assert.Equal(2, snapshot.Count);
    }

    [Fact]
    public async Task LobbyRemoved_DeregistersLobbyAndBroadcastsNothing()
    {
        var (context, broadcaster, _, tick) = await StartedRace();
        context.Time.Advance(CountdownEnd);
        await context.Lobbies.Mutate(context.LobbyId, l =>
        {
            foreach (var id in l.GetPlayerIds().ToList()) l.Disconnect(id);
        });
        await context.Lobbies.RemoveIfEmpty(context.LobbyId);

        await tick.Tick();

        Assert.Empty(context.Registry.GetRacingLobbies());
        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task RaceFinishes_DeregistersLobbyAndStopsRaceState()
    {
        var (context, broadcaster, _, tick) = await RacingRace();
        LobbyFactory.WireRaceFinished(context);
        foreach (var playerId in await context.ReadLobby(l => l.GetPlayerIds().ToList()))
            await context.Races.Mutate(context.LobbyId, r => r.WithdrawParticipant(playerId));

        await tick.Tick();

        Assert.Empty(context.Registry.GetRacingLobbies());
        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task LobbyLeavesRegistry_PrunesConflator()
    {
        var (context, _, conflator, tick) = await RacingRace();
        await tick.Tick();
        Assert.Equal(1, conflator.TrackedLobbies);
        context.Registry.DeregisterLobby(context.LobbyId);

        await tick.Tick();

        Assert.Equal(0, conflator.TrackedLobbies);
    }

    private static async Task<(LobbyTestContext Context, FakeBroadcaster Broadcaster, RaceStateConflator Conflator, RaceTickService Tick)> StartedRace()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        var broadcaster = new FakeBroadcaster();
        var conflator = new RaceStateConflator(broadcaster, NullLogger<RaceStateConflator>.Instance);

        return (context, broadcaster, conflator, LobbyFactory.TickService(context, broadcaster, conflator));
    }

    private static async Task<(LobbyTestContext Context, FakeBroadcaster Broadcaster, RaceStateConflator Conflator, RaceTickService Tick)> RacingRace()
    {
        var race = await StartedRace();
        race.Context.Time.Advance(CountdownEnd);
        await race.Tick.Tick();
        race.Broadcaster.Broadcasts.Clear();

        return race;
    }

    private static async Task<bool> HasStarted(LobbyTestContext context)
    {
        var started = false;
        await context.Races.TryMutate(context.LobbyId, r => started = r.HasStarted);
        return started;
    }
}
