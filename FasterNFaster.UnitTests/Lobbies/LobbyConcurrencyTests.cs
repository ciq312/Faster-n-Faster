using System.Collections.Concurrent;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Lobbies.Colors;
using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;
using FasterNFaster.Api.UseCases.Lobbies.LobbyState;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;

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
            Assert.Null(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
            Assert.Empty(context.Registry.GetRacingLobbies());
            Assert.All(players, u => Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(u.Id)));
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
            var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var sweep = LobbyFactory.SweepService(context, time);
            time.Advance(LobbyFactory.EmptyLobbyTtl);

            await ConcurrentRunner.RunTogether([
                () => sweep.Sweep(CancellationToken.None),
                () => JoinIgnoringNotFound(context, joiner)
            ]);

            if (!context.LobbyExists)
            {
                Assert.Null(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
                Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(joiner.Id));
            }
            else
            {
                var (playerIds, hostId) = await context.ReadLobby(l => (l.GetPlayerIds().ToList(), l.HostId));
                Assert.Equal(joiner.Id, Assert.Single(playerIds));
                Assert.Equal(joiner.Id, hostId);
                Assert.NotNull(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
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

        var joinHandler = new JoinLobbyHandler(context.LobbyAccess);
        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.RaceAccess);
        var lobbiesHandler = new GetLobbiesHandler(context.LobbyAccess, context.RaceAccess);

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
                context.LobbyAccess.TryRead(context.LobbyId, l => l.GetPlayerIds())));
            actions.Add(() =>
            {
                var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, new FakeBroadcaster());
                return Repeat(readsPerReader, () => scope.Run(() =>
                {
                    context.Tracker.MarkChanged(context.LobbyId);
                    return Task.CompletedTask;
                }));
            });
        }

        return actions;
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
        var handler = new JoinLobbyHandler(context.LobbyAccess);
        return ConcurrentRunner.RunTogether(joiners.Select<User, Func<Task>>(u =>
            () => handler.Handle(new JoinLobbyCommand(u.Id, context.LobbyId, u.Nick), CancellationToken.None)));
    }

    private static async Task JoinIgnoringNotFound(LobbyTestContext context, User joiner)
    {
        try
        {
            await new JoinLobbyHandler(context.LobbyAccess)
                .Handle(new JoinLobbyCommand(joiner.Id, context.LobbyId, joiner.Nick), CancellationToken.None);
        }
        catch (LobbyNotFoundException)
        {
        }
    }

    private static Task DisconnectTogether(LobbyTestContext context, IEnumerable<User> leavers)
    {
        var handler = new DisconnectHandler(context.LobbyAccess, context.RaceAccess);
        return ConcurrentRunner.RunTogether(leavers.Select<User, Func<Task>>(u =>
            () => handler.Handle(new DisconnectCommand(u.Id), CancellationToken.None)));
    }

    private static async Task AssertRegistryMatchesLobby(LobbyTestContext context, IEnumerable<User> gone)
    {
        foreach (var playerId in await context.ReadLobby(l => l.GetPlayerIds().ToList()))
            Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(playerId));

        foreach (var user in gone)
            Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(user.Id));
    }
}
