using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Factories.Implementations;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;
using FasterNFaster.Tests;
using FasterNFaster.Tests.Fakes;
using Microsoft.VisualBasic;

public class FastReconnectHandlerTests
{
    [Fact]
    public async Task FastReconnect_ShouldReconnect()
    {
        var (store, lobbyService, registry, lobby) = TestData.SetupWithPlayers(
            new User("Player1", "player1", "pass"),
            new User("Player2", "player2", "pass"));

        var userRepo = new FakeUserRepository();
        var userFactory = new UserFactory(userRepo);
        var handler = new FastReconnectHandler(lobbyService);
        var playerId = lobby.Players.ElementAt(0).User.Id;

        // Start reconnect timer but don't await — let it run in the background
        var reconnectTask = handler.Handle(new FastReconnectCommand(lobby.Id, playerId));

        // Simulate the player reconnecting — this cancels the pending removal
        var joinHandler = new JoinLobbyHandler(store, userFactory, lobbyService);
        var result = await joinHandler.Handle(new JoinLobbyCommand(playerId, lobby.Id, "test", "Guest"));

        Assert.True(result.IsReconnect);
        await Assert.ThrowsAsync<TaskCanceledException>(() => reconnectTask);
    }
}
