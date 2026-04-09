using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;
using FasterNFaster.Tests.Fakes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

public class KickLobbyHandlerTests
{

    private (KickPlayerHandler handler, FakeLobbyStore store, FakeLobbyService lobbyService, FakeRaceTickRegistry registry, Lobby lobby)
           SetupWithPlayers(params User[] users)
    {
        var store = new FakeLobbyStore();
        var lobbyService = new FakeLobbyService();
        var registry = new FakeRaceTickRegistry();
        var handler = new KickPlayerHandler(store, lobbyService, registry);

        var lobby = new Lobby("Test", false, new WordRace(50));
        lobby.AssignHost(users[0].Id);
        foreach (var user in users)
            lobby.AddPlayer(user);
        store.Seed(lobby);

        return (handler, store, lobbyService, registry, lobby);
    }
    [Fact]
    public void KickPlayer_ShouldNotBeInLobby()
    {
        var hostUser = new User("host");
        var playerToKick = new User("player");

        (var handler, var store, var lobbyService, var tickRegistry, var lobby)
         = SetupWithPlayers([hostUser, playerToKick]);

        handler.Handle(new KickPlayerCommand(hostUser.Id, lobby.Id, playerToKick.Id));

        Assert.False(lobby.IsPlayerInLobby(playerToKick.Id));
        Assert.Single(lobby.Players);
    }
}
