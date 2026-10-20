using System.Collections.Concurrent;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Lobbies.Colors;
using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;
using FasterNFaster.Api.UseCases.Lobbies.LobbyState;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Lobbies;

public class LobbyConcurrencyTests
{
    private const int Rounds = 50;
    private static readonly TimeSpan RoundTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task ParallelJoins_EachPlayerJoinsOnceWithUniqueOrderAndColor()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var host = new User("host");
            var context = await LobbyFactory.WithPlayers(host);
            var maxPlayers = await context.ReadLobby(l => l.LobbySettings.MaxPlayers);
            var joiners = Players(maxPlayers - 1);

            await JoinTogether(context, joiners);

            var (players, hostId) = await context.ReadLobby(l => (l.Players.ToList(), l.HostId));
            Assert.Equal(joiners.Select(u => u.Id).Append(host.Id).ToHashSet(), players.Select(p => p.Id).ToHashSet());
            Assert.Equal(maxPlayers, players.Count);
            Assert.Equal(Enumerable.Range(1, maxPlayers), players.Select(p => p.JoinOrder).Order());
            Assert.Equal(maxPlayers, players.Select(p => p.Color).Distinct().Count());
            Assert.All(players, p => Assert.Contains(p.Color, PlayerColors.Palette));
            Assert.Equal(host.Id, hostId);
            await AssertRegistryMatchesLobby(context, []);
            Assert.Equal(maxPlayers, context.Dispatcher.Dispatched.OfType<PlayerJoinedEvent>().Count());
        }
    }

    [Fact]
    public async Task ParallelJoins_WhenEachPlayerJoinsTwice_AddsEachOnce()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var host = new User("host");
            var context = await LobbyFactory.WithPlayers(host);
            var maxPlayers = await context.ReadLobby(l => l.LobbySettings.MaxPlayers);
            var joiners = Players(maxPlayers - 1);

            await JoinTogether(context, joiners.Concat(joiners));

            var players = await context.ReadLobby(l => l.Players.ToList());
            Assert.Equal(joiners.Select(u => u.Id).Append(host.Id).ToHashSet(), players.Select(p => p.Id).ToHashSet());
            Assert.Equal(maxPlayers, players.Count);
            Assert.Equal(maxPlayers, players.Select(p => p.JoinOrder).Distinct().Count());
            Assert.Equal(maxPlayers, context.Dispatcher.Dispatched.OfType<PlayerJoinedEvent>().Count());
            await AssertRegistryMatchesLobby(context, []);
        }
    }

    [Fact]
    public async Task JoinTwoLobbiesTogether_PlayerEndsUpInExactlyOne()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var context = await LobbyFactory.WithPlayers(new User("host"));
            var (otherLobbyId, _) = await context.Lobbies.Create("Other", false, Guid.NewGuid());
            var joiner = new User("joiner");
            var handler = new JoinLobbyHandler(context.Lobbies);
            var releaseFirst = await HoldGate(context.Lobbies, context.LobbyId);
            var releaseSecond = await HoldGate(context.Lobbies, otherLobbyId);

            var joinFirst = Record.ExceptionAsync(() =>
                handler.Handle(new JoinLobbyCommand(joiner.Id, context.LobbyId, joiner.Nick), CancellationToken.None));
            var joinSecond = Record.ExceptionAsync(() =>
                handler.Handle(new JoinLobbyCommand(joiner.Id, otherLobbyId, joiner.Nick), CancellationToken.None));
            releaseFirst();
            releaseSecond();
            var outcomes = await Task.WhenAll(joinFirst, joinSecond);

            Assert.Single(outcomes, e => e is null);
            Assert.Single(outcomes, e => e is AlreadyInLobbyException);
            var inFirst = await context.ReadLobby(l => l.IsPlayerIn(joiner.Id));
            var inSecond = await context.Lobbies.Read(otherLobbyId, l => l.IsPlayerIn(joiner.Id));
            Assert.True(inFirst ^ inSecond);
            Assert.Equal(inFirst ? context.LobbyId : otherLobbyId, context.Lobbies.GetLobbyIdOfPlayer(joiner.Id));
        }
    }

    [Fact]
    public async Task HostAndOthersDisconnectTogether_LowestJoinOrderSurvivorBecomesHost()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var players = Players(10);
            var context = await LobbyFactory.WithPlayers(players);
            LobbyFactory.WireCleanup(context);
            var leavers = players[..6];
            var survivors = players[6..];

            await DisconnectTogether(context, leavers);

            Assert.True(context.LobbyExists);
            var (remaining, hostId) = await context.ReadLobby(l => (l.Players.ToList(), l.HostId));
            Assert.Equal(survivors.Select(u => u.Id).ToHashSet(), remaining.Select(p => p.Id).ToHashSet());
            Assert.Equal(players[6].Id, hostId);
            Assert.Contains(remaining, p => p.Id == hostId);
            await AssertRegistryMatchesLobby(context, leavers);
            Assert.Equal(leavers.Length, context.Dispatcher.Dispatched.OfType<PlayerRemovedEvent>().Count());
        }
    }

    [Fact]
    public async Task DisconnectTwiceTogether_SecondIsNoOp()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var (_, other, context) = await LobbyFactory.TwoUsersSetup();
            var release = await HoldGate(context.Lobbies, context.LobbyId);

            var disconnects = DisconnectBoth(context, other, release);
            var exceptions = await disconnects;

            Assert.All(exceptions, Assert.Null);
            Assert.Equal(1, context.Dispatcher.Dispatched.OfType<PlayerRemovedEvent>().Count(e => e.UserId == other.Id));
            Assert.Single(await context.ReadLobby(l => l.GetPlayerIds().ToList()));
        }
    }

    [Fact]
    public async Task LastPlayerDisconnectsTwiceTogether_RemovesLobbyWithoutThrowing()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var host = new User("host");
            var context = await LobbyFactory.WithPlayers(host);
            LobbyFactory.WireCleanup(context);
            var release = await HoldGate(context.Lobbies, context.LobbyId);

            var exceptions = await DisconnectBoth(context, host, release);

            Assert.All(exceptions, Assert.Null);
            Assert.False(context.LobbyExists);
        }
    }

    [Fact]
    public async Task EveryoneDisconnectsTogether_RemovesLobbyWithoutThrowing()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var players = Players(10);
            var context = await LobbyFactory.WithPlayers(players);
            LobbyFactory.WireCleanup(context);

            var exception = await Record.ExceptionAsync(() => DisconnectTogether(context, players));

            Assert.Null(exception);
            Assert.False(context.LobbyExists);
            Assert.False(context.Races.Exists(context.LobbyId));
            Assert.Empty(context.Registry.GetRacingLobbies());
            Assert.All(players, u => Assert.Null(context.Lobbies.GetLobbyIdOfPlayer(u.Id)));
        }
    }

    [Fact]
    public async Task SweepAndJoinTogether_LeavesLobbyConsistent()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var creator = Guid.NewGuid();
            var joiner = new User("joiner");
            var context = await LobbyFactory.Empty(creator);
            var sweep = LobbyFactory.SweepService(context);
            context.Time.Advance(LobbyFactory.EmptyLobbyTtl);

            await ConcurrentRunner.RunTogether([
                () => sweep.Sweep(CancellationToken.None),
                () => JoinIgnoringNotFound(context, joiner)
            ]);

            if (!context.LobbyExists)
            {
                Assert.False(context.Races.Exists(context.LobbyId));
                Assert.Null(context.Lobbies.GetLobbyIdOfPlayer(joiner.Id));
            }
            else
            {
                var (playerIds, hostId) = await context.ReadLobby(l => (l.GetPlayerIds().ToList(), l.HostId));
                Assert.Equal(joiner.Id, Assert.Single(playerIds));
                Assert.Equal(joiner.Id, hostId);
                Assert.True(context.Races.Exists(context.LobbyId));
            }
        }
    }

    [Fact]
    public async Task JoinsAndLeavesWithStateReads_NoReaderThrows()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var players = Players(10);
            var context = await LobbyFactory.WithPlayers(players[..5]);
            LobbyFactory.WireCleanup(context);
            var leavers = players[1..5];

            var exception = await Record.ExceptionAsync(() => ConcurrentRunner
                .RunTogether(ReadersAndWriters(context, players[5..], leavers, []))
                .WaitAsync(RoundTimeout));

            Assert.Null(exception);
            Assert.Equal(
                players[5..].Select(u => u.Id).Append(players[0].Id).ToHashSet(),
                (await context.ReadLobby(l => l.GetPlayerIds().ToList())).ToHashSet());
            await AssertRegistryMatchesLobby(context, leavers);
        }
    }

    [Fact]
    public async Task JoinsAndLeavesWithStateReads_EveryStateIsConsistentSnapshot()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var players = Players(10);
            var context = await LobbyFactory.WithPlayers(players[..5]);
            LobbyFactory.WireCleanup(context);
            ConcurrentBag<LobbyStateDTO> states = [];

            await ConcurrentRunner
                .RunTogether(ReadersAndWriters(context, players[5..], players[1..5], states))
                .WaitAsync(RoundTimeout);

            Assert.NotEmpty(states);
            foreach (var state in states)
            {
                Assert.Equal(state.Players.Count, state.Colors.Count(c => !c.IsAvailable));
                Assert.Equal(state.Players.Count, state.Players.Select(p => p.Color).Distinct().Count());
                if (state.Players.Count > 0)
                    Assert.Single(state.Players, p => p.IsHost);
            }
        }
    }

    private static List<Func<Task>> ReadersAndWriters(
        LobbyTestContext context,
        User[] joiners,
        User[] leavers,
        ConcurrentBag<LobbyStateDTO> states)
    {
        const int readersPerKind = 5;
        const int readsPerReader = 20;

        var joinHandler = new JoinLobbyHandler(context.Lobbies);
        var disconnectHandler = new DisconnectHandler(context.Lobbies, context.Races);
        var lobbiesHandler = new GetLobbiesHandler(context.Lobbies, context.Races);

        List<Func<Task>> actions = [];
        actions.AddRange(joiners.Select<User, Func<Task>>(u =>
            () => joinHandler.Handle(new JoinLobbyCommand(u.Id, context.LobbyId, u.Nick), CancellationToken.None)));
        actions.AddRange(leavers.Select<User, Func<Task>>(u =>
            () => disconnectHandler.Handle(new DisconnectCommand(u.Id), CancellationToken.None)));

        for (var i = 0; i < readersPerKind; i++)
        {
            actions.Add(() => Repeat(readsPerReader, async () =>
                states.Add(await context.LobbyQuery.GetLobbyState(context.LobbyId))));
            actions.Add(() => Repeat(readsPerReader, () =>
                lobbiesHandler.Handle(new GetLobbiesQuery(), CancellationToken.None)));
            actions.Add(() => Repeat(readsPerReader, () =>
                context.Lobbies.TryRead(context.LobbyId, l => l.GetPlayerIds())));
            actions.Add(() =>
            {
                var scope = new LobbyStateScope(context.Tracker, context.Lobbies, context.LobbyQuery, new FakeBroadcaster());
                return Repeat(readsPerReader, () => scope.Run(() =>
                {
                    context.Tracker.MarkChanged(context.LobbyId);
                    return Task.CompletedTask;
                }));
            });
        }

        return actions;
    }

    private static Task<Exception?[]> DisconnectBoth(LobbyTestContext context, User player, Action releaseGate)
    {
        var handler = new DisconnectHandler(context.Lobbies, context.Races);
        var first = Record.ExceptionAsync(() => handler.Handle(new DisconnectCommand(player.Id), CancellationToken.None));
        var second = Record.ExceptionAsync(() => handler.Handle(new DisconnectCommand(player.Id), CancellationToken.None));
        releaseGate();

        return Task.WhenAll(first, second);
    }

    private static async Task<Action> HoldGate(ILobbyStore lobbies, Guid lobbyId)
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new ManualResetEventSlim();
        _ = Task.Run(() => lobbies.Mutate(lobbyId, _ =>
        {
            held.SetResult();
            release.Wait();
        }));
        await held.Task;

        return release.Set;
    }

    private static async Task Repeat(int times, Func<Task> action)
    {
        for (var i = 0; i < times; i++)
            await action();
    }

    private static User[] Players(int count) =>
        Enumerable.Range(0, count).Select(i => new User($"player{i}")).ToArray();

    private static Task JoinTogether(LobbyTestContext context, IEnumerable<User> joiners)
    {
        var handler = new JoinLobbyHandler(context.Lobbies);
        return ConcurrentRunner.RunTogether(joiners.Select<User, Func<Task>>(u =>
            () => handler.Handle(new JoinLobbyCommand(u.Id, context.LobbyId, u.Nick), CancellationToken.None)));
    }

    private static async Task JoinIgnoringNotFound(LobbyTestContext context, User joiner)
    {
        try
        {
            await new JoinLobbyHandler(context.Lobbies)
                .Handle(new JoinLobbyCommand(joiner.Id, context.LobbyId, joiner.Nick), CancellationToken.None);
        }
        catch (LobbyNotFoundException)
        {
        }
    }

    private static Task DisconnectTogether(LobbyTestContext context, IEnumerable<User> leavers)
    {
        var handler = new DisconnectHandler(context.Lobbies, context.Races);
        return ConcurrentRunner.RunTogether(leavers.Select<User, Func<Task>>(u =>
            () => handler.Handle(new DisconnectCommand(u.Id), CancellationToken.None)));
    }

    private static async Task AssertRegistryMatchesLobby(LobbyTestContext context, IEnumerable<User> gone)
    {
        foreach (var playerId in await context.ReadLobby(l => l.GetPlayerIds().ToList()))
            Assert.Equal(context.LobbyId, context.Lobbies.GetLobbyIdOfPlayer(playerId));

        foreach (var user in gone)
            Assert.Null(context.Lobbies.GetLobbyIdOfPlayer(user.Id));
    }
}
