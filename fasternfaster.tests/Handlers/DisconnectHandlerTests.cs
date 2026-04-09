using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class DisconnectHandlerTests
{
    private (DisconnectHandler handler, FakeLobbyStore store, FakeLobbyService lobbyService, FakeRaceTickRegistry registry, Lobby lobby)
        SetupWithPlayers(params User[] users)
    {
        var store = new FakeLobbyStore();
        var lobbyService = new FakeLobbyService();
        var registry = new FakeRaceTickRegistry();
        var handler = new DisconnectHandler(store, lobbyService, registry);

        var lobby = new Lobby("Test", false, new WordRace(50));
        lobby.AssignHost(users[0].Id);
        foreach (var user in users)
            lobby.AddPlayer(user);
        store.Seed(lobby);

        return (handler, store, lobbyService, registry, lobby);
    }

    [Fact]
    public async Task Disconnect_LobbyNotFound_ShouldThrow()
    {
        var store = new FakeLobbyStore();
        var lobbyService = new FakeLobbyService();
        var registry = new FakeRaceTickRegistry();
        var handler = new DisconnectHandler(store, lobbyService, registry);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new DisconnectCommand("conn1", Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task Disconnect_PlayerNotInLobby_ShouldThrow()
    {
        var store = new FakeLobbyStore();
        var lobbyService = new FakeLobbyService();
        var registry = new FakeRaceTickRegistry();
        var handler = new DisconnectHandler(store, lobbyService, registry);
        var lobby = new Lobby("Test", false, new WordRace(50));
        store.Seed(lobby);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new DisconnectCommand("conn1", lobby.Id, Guid.NewGuid())));
    }

    [Fact]
    public async Task Disconnect_ShouldRemovePlayerFromLobby()
    {
        var host = new User("Host", "host", "pass");
        var (handler, store, _, _, lobby) = SetupWithPlayers(host);

        await handler.Handle(new DisconnectCommand("conn1", lobby.Id, host.Id));

        Assert.False(lobby.IsPlayerInLobby(host.Id));
    }

    [Fact]
    public async Task Disconnect_HostLeaves_ShouldPromoteNextPlayer()
    {
        var host = new User("Host", "hostlogin", "password");
        var player2 = new User("Player2", "player2login", "password");
        var (handler, _, _, _, lobby) = SetupWithPlayers(host, player2);

        var result = await handler.Handle(new DisconnectCommand("conn1", lobby.Id, host.Id));

        Assert.Equal(player2.Id, result.NewHostId);
        Assert.Equal(player2.Id, lobby.HostId);
        Assert.False(lobby.IsPlayerInLobby(host.Id));
    }

    [Fact]
    public async Task Disconnect_NonHost_ShouldNotChangeHost()
    {
        var host = new User("Host", "hostlogin", "password");
        var player2 = new User("Player2", "player2login", "password");
        var (handler, _, _, _, lobby) = SetupWithPlayers(host, player2);

        var result = await handler.Handle(new DisconnectCommand("conn1", lobby.Id, player2.Id));

        Assert.Null(result.NewHostId);
        Assert.Equal(host.Id, lobby.HostId);
        Assert.False(lobby.IsPlayerInLobby(player2.Id));
    }

    [Fact]
    public async Task Disconnect_LastPlayerDuringRace_ShouldDeregisterTicks()
    {
        var host = new User("Host", "hostlogin", "password");
        var (handler, _, _, registry, lobby) = SetupWithPlayers(host);

        lobby.Race.Start(lobby.Players.Where(p => p.IsConnected).Select(p => (p.User.Id, p.Color, p.User.Nick)));
        registry.RegisterLobby(lobby.Id);

        var result = await handler.Handle(new DisconnectCommand("conn1", lobby.Id, host.Id));

        Assert.True(result.ShouldDeregisterTicks);
        Assert.False(registry.IsRegistered(lobby.Id));
    }

    [Fact]
    public async Task Disconnect_OtherPlayersStillConnectedDuringRace_ShouldNotDeregister()
    {
        var host = new User("Host", "hostlogin", "password");
        var player2 = new User("Player2", "player2login", "password");
        var (handler, _, _, registry, lobby) = SetupWithPlayers(host, player2);

        lobby.Race.Start(lobby.Players.Where(p => p.IsConnected).Select(p => (p.User.Id, p.Color, p.User.Nick)));
        registry.RegisterLobby(lobby.Id);

        var result = await handler.Handle(new DisconnectCommand("conn1", lobby.Id, host.Id));

        Assert.False(result.ShouldDeregisterTicks);
        Assert.True(registry.IsRegistered(lobby.Id));
    }
    [Fact]
    public async Task Disconnect_LastPlayerInLobby_ShouldRemoveLobby()
    {
        var host = new User("Host", "hostlogin", "password");
        var (handler, store, lobbyService, _, lobby) = SetupWithPlayers(host);
        var result = await handler.Handle(new DisconnectCommand("conn1", lobby.Id, host.Id));

        Assert.Null(store.Get(lobby.Id));
    }
}
