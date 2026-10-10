using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;
using Microsoft.Extensions.Time.Testing;

namespace FasterNFaster.Tests.Handlers;

public class LobbyConcurrencyTests
{
    private const int Rounds = 50;

    [Fact]
    public async Task ParallelJoins_EachPlayerJoinsOnceWithUniqueOrderAndColor()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var host = new User("host");
            var context = await LobbyFactory.WithPlayers(host);
            var maxPlayers = context.Lobby.LobbySettings.MaxPlayers;
            var joiners = Players(maxPlayers - 1);

            await JoinTogether(context, joiners);

            var players = context.Lobby.Players;
            Assert.Equal(joiners.Select(u => u.Id).Append(host.Id).ToHashSet(), players.Select(p => p.Id).ToHashSet());
            Assert.Equal(maxPlayers, players.Count);
            Assert.Equal(Enumerable.Range(1, maxPlayers), players.Select(p => p.JoinOrder).Order());
            Assert.Equal(maxPlayers, players.Select(p => p.Color).Distinct().Count());
            Assert.All(players, p => Assert.Contains(p.Color, PlayerColors.Palette));
            Assert.Equal(host.Id, context.Lobby.HostId);
            AssertRegistryMatchesLobby(context, []);
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
            var maxPlayers = context.Lobby.LobbySettings.MaxPlayers;
            var joiners = Players(maxPlayers - 1);

            await JoinTogether(context, joiners.Concat(joiners));

            var players = context.Lobby.Players;
            Assert.Equal(joiners.Select(u => u.Id).Append(host.Id).ToHashSet(), players.Select(p => p.Id).ToHashSet());
            Assert.Equal(maxPlayers, players.Count);
            Assert.Equal(maxPlayers, players.Select(p => p.JoinOrder).Distinct().Count());
            Assert.Equal(maxPlayers, context.Dispatcher.Dispatched.OfType<PlayerJoinedEvent>().Count());
            AssertRegistryMatchesLobby(context, []);
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

            Assert.NotNull(context.Store.Get(context.LobbyId));
            Assert.Equal(survivors.Select(u => u.Id).ToHashSet(), context.Lobby.Players.Select(p => p.Id).ToHashSet());
            Assert.Equal(players[6].Id, context.Lobby.HostId);
            Assert.Contains(context.Lobby.Players, p => p.Id == context.Lobby.HostId);
            AssertRegistryMatchesLobby(context, leavers);
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
            Assert.Null(context.Store.Get(context.LobbyId));
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

            if (context.Store.Get(context.LobbyId) is null)
            {
                Assert.Null(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
                Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(joiner.Id));
            }
            else
            {
                Assert.Equal(joiner.Id, Assert.Single(context.Lobby.Players).Id);
                Assert.Equal(joiner.Id, context.Lobby.HostId);
                Assert.NotNull(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
            }
        }
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

    private static void AssertRegistryMatchesLobby(LobbyTestContext context, IEnumerable<User> gone)
    {
        foreach (var player in context.Lobby.Players)
            Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(player.Id));

        foreach (var user in gone)
            Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(user.Id));
    }
}
