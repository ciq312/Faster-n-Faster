using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Tests.Fakes;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Tests;

public static class TestData
{
    public static (FakeLobbyStore store, FakeLobbyService lobbyService, FakeRaceTickRegistry registry, Lobby lobby)
           SetupWithPlayers(params User[] users)
    {
        var store = new FakeLobbyStore();
        var lobbyService = new FakeLobbyService();
        var registry = new FakeRaceTickRegistry();

        var lobby = new Lobby("Test", false, new WordRace(50));
        lobby.AssignHost(users[0].Id);
        foreach (var user in users)
            lobby.AddPlayer(user);
        store.Seed(lobby);

        return (store, lobbyService, registry, lobby);
    }
}
