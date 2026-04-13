using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Services;

namespace FasterNFaster.Tests.Handlers;

public class DisconnectHandlerTests
{
    [Fact]
    public async Task Disconnect_LobbyNotFound_ShouldThrow()
    {
        var store = new LobbyStore();
        var lobbyService = new LobbyService();
        var registry = new RaceTickRegistry();
        var handler = new DisconnectHandler(store, lobbyService, registry);

        var connId = "conn-missing";
        lobbyService.TrackConnection(connId, Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new DisconnectCommand(connId, Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task Disconnect_PlayerNotInLobby_ShouldThrow()
    {
        var ctx = await LobbyFactory.Empty(Guid.NewGuid());
        var handler = new DisconnectHandler(ctx.Store, ctx.LobbyService, ctx.Registry);

        var unknownPlayerId = Guid.NewGuid();
        ctx.LobbyService.TrackConnection("conn-fake", ctx.LobbyId, unknownPlayerId);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new DisconnectCommand("conn-fake", ctx.LobbyId, unknownPlayerId)));
    }

    [Fact]
    public async Task Disconnect_ShouldRemovePlayerFromLobby()
    {
        var host = new User("Host", "hostlogin", "password");
        var ctx = await LobbyFactory.WithPlayers(host);
        var handler = new DisconnectHandler(ctx.Store, ctx.LobbyService, ctx.Registry);
        var lobby = ctx.Lobby;

        var connId = ctx.LobbyService.GetConnectionId(ctx.LobbyId, host.Id)!;
        await handler.Handle(new DisconnectCommand(connId, ctx.LobbyId, host.Id));

        Assert.False(lobby.IsPlayerInLobby(host.Id));
    }

    [Fact]
    public async Task Disconnect_HostLeaves_ShouldPromoteNextPlayer()
    {
        var host = new User("Host", "hostlogin", "password");
        var player2 = new User("Player2", "player2login", "password");
        var ctx = await LobbyFactory.WithPlayers(host, player2);
        var handler = new DisconnectHandler(ctx.Store, ctx.LobbyService, ctx.Registry);

        var connId = ctx.LobbyService.GetConnectionId(ctx.LobbyId, host.Id)!;
        var result = await handler.Handle(new DisconnectCommand(connId, ctx.LobbyId, host.Id));

        Assert.Equal(player2.Id, result.NewHostId);
        Assert.Equal(player2.Id, ctx.Lobby.HostId);
        Assert.False(ctx.Lobby.IsPlayerInLobby(host.Id));
    }

    [Fact]
    public async Task Disconnect_NonHost_ShouldNotChangeHost()
    {
        var host = new User("Host", "hostlogin", "password");
        var player2 = new User("Player2", "player2login", "password");
        var ctx = await LobbyFactory.WithPlayers(host, player2);
        var handler = new DisconnectHandler(ctx.Store, ctx.LobbyService, ctx.Registry);

        var connId = ctx.LobbyService.GetConnectionId(ctx.LobbyId, player2.Id)!;
        var result = await handler.Handle(new DisconnectCommand(connId, ctx.LobbyId, player2.Id));

        Assert.Null(result.NewHostId);
        Assert.Equal(host.Id, ctx.Lobby.HostId);
        Assert.False(ctx.Lobby.IsPlayerInLobby(player2.Id));
    }

    [Fact]
    public async Task Disconnect_LastPlayerDuringRace_ShouldDeregisterTicks()
    {
        var host = new User("Host", "hostlogin", "password");
        var ctx = await LobbyFactory.WithPlayers(host);
        var handler = new DisconnectHandler(ctx.Store, ctx.LobbyService, ctx.Registry);

        var lobby = ctx.Lobby;
        lobby.Race.Start(lobby.Players.Where(p => p.IsConnected).Select(p => (p.User.Id, p.Color, p.User.Nick)));
        ctx.Registry.RegisterLobby(ctx.LobbyId);

        var connId = ctx.LobbyService.GetConnectionId(ctx.LobbyId, host.Id)!;
        var result = await handler.Handle(new DisconnectCommand(connId, ctx.LobbyId, host.Id));

        Assert.True(result.ShouldDeregisterTicks);
        Assert.DoesNotContain(ctx.Registry.GetRacingLobbies(), e => e.LobbyId == ctx.LobbyId);
    }

    [Fact]
    public async Task Disconnect_OtherPlayersStillConnectedDuringRace_ShouldNotDeregister()
    {
        var host = new User("Host", "hostlogin", "password");
        var player2 = new User("Player2", "player2login", "password");
        var ctx = await LobbyFactory.WithPlayers(host, player2);
        var handler = new DisconnectHandler(ctx.Store, ctx.LobbyService, ctx.Registry);

        var lobby = ctx.Lobby;
        lobby.Race.Start(lobby.Players.Where(p => p.IsConnected).Select(p => (p.User.Id, p.Color, p.User.Nick)));
        ctx.Registry.RegisterLobby(ctx.LobbyId);

        var connId = ctx.LobbyService.GetConnectionId(ctx.LobbyId, host.Id)!;
        var result = await handler.Handle(new DisconnectCommand(connId, ctx.LobbyId, host.Id));

        Assert.False(result.ShouldDeregisterTicks);
        Assert.Contains(ctx.Registry.GetRacingLobbies(), e => e.LobbyId == ctx.LobbyId);
    }

    [Fact]
    public async Task Disconnect_LastPlayerInLobby_ShouldRemoveLobby()
    {
        var host = new User("Host", "hostlogin", "password");
        var ctx = await LobbyFactory.WithPlayers(host);
        var handler = new DisconnectHandler(ctx.Store, ctx.LobbyService, ctx.Registry);

        var connId = ctx.LobbyService.GetConnectionId(ctx.LobbyId, host.Id)!;
        await handler.Handle(new DisconnectCommand(connId, ctx.LobbyId, host.Id));

        Assert.Null(ctx.Store.Get(ctx.LobbyId));
    }
}
