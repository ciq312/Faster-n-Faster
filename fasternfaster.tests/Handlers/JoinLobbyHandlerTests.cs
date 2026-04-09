using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class JoinLobbyHandlerTests
{
    [Fact]
    public async Task JoinLobby_LobbyNotFound_ShouldThrow()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var lobbyService = new FakeLobbyService();
        var handler = new JoinLobbyHandler(store, userRepo, lobbyService);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new JoinLobbyCommand(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task JoinLobby_UserNotFound_ShouldThrow()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var lobby = new Lobby("Test", false, new WordRace(50));
        store.Seed(lobby);
        var lobbyService = new FakeLobbyService();
        var handler = new JoinLobbyHandler(store, userRepo, lobbyService);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.Handle(new JoinLobbyCommand(Guid.NewGuid(), lobby.Id)));
    }

    [Fact]
    public async Task JoinLobby_ValidPlayerAndLobby_ShouldAddPlayer()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var user = new User("Player1", "login1", "pass");
        userRepo.Seed(user);
        var lobby = new Lobby("Test", false, new WordRace(50));
        lobby.AssignHost(Guid.NewGuid());
        store.Seed(lobby);
        var lobbyService = new FakeLobbyService();
        var handler = new JoinLobbyHandler(store, userRepo, lobbyService);

        await handler.Handle(new JoinLobbyCommand(user.Id, lobby.Id));

        Assert.True(lobby.IsPlayerInLobby(user.Id));
    }

    [Fact]
    public async Task JoinLobby_PlayerAlreadyInLobby_ShouldNotDuplicate()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var user = new User("Player1", "login1", "pass");
        userRepo.Seed(user);
        var lobby = new Lobby("Test", false, new WordRace(50));
        lobby.AssignHost(Guid.NewGuid());
        store.Seed(lobby);
        var lobbyService = new FakeLobbyService();
        var handler = new JoinLobbyHandler(store, userRepo, lobbyService);

        await handler.Handle(new JoinLobbyCommand(user.Id, lobby.Id));
        await handler.Handle(new JoinLobbyCommand(user.Id, lobby.Id));

        Assert.Single(lobby.Players.Where(p => p.User.Id == user.Id));
    }

    [Fact]
    public async Task JoinLobby_LobbyIsFull_ShouldThrow()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var lobby = new Lobby("Test", false, new WordRace(50));
        lobby.AssignHost(Guid.NewGuid());
        store.Seed(lobby);
        var lobbyService = new FakeLobbyService();
        var handler = new JoinLobbyHandler(store, userRepo, lobbyService);

        for (int i = 0; i < lobby.LobbySettings.MaxPlayers; i++)
        {
            var player = new User($"Player{i + 1}", $"login{i + 1}", "pass");
            userRepo.Seed(player);
            await handler.Handle(new JoinLobbyCommand(player.Id, lobby.Id));
        }
        var extraPlayer = new User("ExtraPlayer", "loginExtra", "pass");
        userRepo.Seed(extraPlayer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new JoinLobbyCommand(extraPlayer.Id, lobby.Id)));
    }

    [Fact]
    public async Task JoinLobby_ShouldNotConnectWhenRacingCountdown()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var tickRegistry = new FakeRaceTickRegistry();
        var lobby = new Lobby("Test", false, new WordRace(50));

        tickRegistry.RegisterLobby(lobby.Id);
        lobby.AssignHost(Guid.NewGuid());
        store.Seed(lobby);


        var lobbyService = new FakeLobbyService();
        var handler = new JoinLobbyHandler(store, userRepo, lobbyService);

        var player = new User($"Player1", $"login1", "pass");
        userRepo.Seed(player);
        await handler.Handle(new JoinLobbyCommand(player.Id, lobby.Id));

        lobby.StartSession();

        int delaySeconds = 2;
        await Task.Delay(delaySeconds * 1000);

        Assert.Equal(RacePhase.Countdown, tickRegistry.GetRacingLobby(lobby.Id).Phase);
        Assert.True(lobby.IsSessionActive);

        var player2 = new User("Player2", "login2", "pass");
        userRepo.Seed(player);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new JoinLobbyCommand(player.Id, lobby.Id)));
    }

    [Fact]
    public async Task JoinLobby_ShouldNotConnectWhenRacing()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var tickRegistry = new FakeRaceTickRegistry();
        var passageProvider = new RandomPassageProvider();
        var lobby = new Lobby("Test", false, new WordRace(50));

        tickRegistry.RegisterLobby(lobby.Id);
        lobby.AssignHost(Guid.NewGuid());
        store.Seed(lobby);


        var lobbyService = new FakeLobbyService();
        var joinHandler = new JoinLobbyHandler(store, userRepo, lobbyService);


        var player = new User($"Player2", $"login2", "pass");
        userRepo.Seed(player);
        await joinHandler.Handle(new JoinLobbyCommand(player.Id, lobby.Id));

        lobby.StartSession();

        int delaySeconds = 1;
        await Task.Delay(delaySeconds * 1000);

        var player2 = new User("Player1", "login1", "pass");
        userRepo.Seed(player);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => joinHandler.Handle(new JoinLobbyCommand(player.Id, lobby.Id)));
    }


    [Fact]
    public async Task JoinLobby_ShouldConnectWhenRacingThenWaiting()
    {
        var store = new FakeLobbyStore();
        var userRepo = new FakeUserRepository();
        var tickRegistry = new FakeRaceTickRegistry();
        var lobby = new Lobby("Test", false, new WordRace(50));

        tickRegistry.RegisterLobby(lobby.Id);
        lobby.AssignHost(Guid.NewGuid());
        store.Seed(lobby);

        var lobbyService = new FakeLobbyService();
        var joinHandler = new JoinLobbyHandler(store, userRepo, lobbyService);

        lobby.Race.Start(lobby.Players.Where(p => p.IsConnected).Select(p => (p.User.Id, p.Color, p.User.Nick)));

        int delaySeconds = 1;
        await Task.Delay(delaySeconds * 1000);


        var player = new User("Player1", "login1", "pass");
        userRepo.Seed(player);

        await joinHandler.Handle(new JoinLobbyCommand(player.Id, lobby.Id));

        Assert.True(lobby.IsPlayerInLobby(player.Id));
    }
}
