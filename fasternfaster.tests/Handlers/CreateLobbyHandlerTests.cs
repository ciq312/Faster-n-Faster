using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class CreateLobbyHandlerTests
{
    [Fact]
    public async Task CreateLobby_ShouldReturnLobbyIdAndName()
    {
        var store = new FakeLobbyStore();
        var passageProvider = new RandomPassageProvider();
        var handler = new CreateLobbyHandler(store, passageProvider);
        var hostId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateLobbyCommand("Test Lobby", false, hostId));

        Assert.Equal("Test Lobby", result.LobbyName);
        Assert.NotEqual(Guid.Empty, result.LobbyId);
    }

    [Fact]
    public async Task CreateLobby_ShouldStoreTheLobby()
    {
        var store = new FakeLobbyStore();
        var passageProvider = new RandomPassageProvider();
        var handler = new CreateLobbyHandler(store, passageProvider);
        var hostId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateLobbyCommand("My Lobby", false, hostId));

        var stored = store.Get(result.LobbyId);
        Assert.NotNull(stored);
        Assert.Equal("My Lobby", stored.Name);
        Assert.Equal(hostId, stored.HostId);
    }
}
