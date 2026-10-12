using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Services;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FasterNFaster.UnitTests.Lobbies;

public class CreateLobbyHandlerTests
{
    [Fact]
    public async Task CreateLobby_ShouldReturnLobbyIdAndName()
    {
        var passageProvider = new RandomPassageProvider();
        var dispatcher = new FakeEventDispatcher();
        var lobbyStore = new InMemoryLobbyRepository();

        var tracker = new LobbyStateTracker();
        var lobbies = new LobbyAccess(lobbyStore, new InMemoryPlayerLocationRegistry(), dispatcher, tracker);
        var races = new RaceAccess(dispatcher, passageProvider, tracker, NullLogger<RaceAccess>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbies, races);

        var hostId = Guid.NewGuid();

        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, hostId), CancellationToken.None);

        var (name, storedHostId) = await lobbies.Read(result.LobbyId, l => (l.Name, l.HostId));

        Assert.Equal("testLobby", name);
        Assert.Equal(hostId, storedHostId);
    }

    [Fact]
    public async Task CreateLobby_ShouldStoreTheLobby()
    {
        var passageProvider = new RandomPassageProvider();
        var dispatcher = new FakeEventDispatcher();
        var lobbyStore = new InMemoryLobbyRepository();

        var tracker = new LobbyStateTracker();
        var lobbies = new LobbyAccess(lobbyStore, new InMemoryPlayerLocationRegistry(), dispatcher, tracker);
        var races = new RaceAccess(dispatcher, passageProvider, tracker, NullLogger<RaceAccess>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbies, races);
        var lobbyId = Guid.NewGuid();

        await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, lobbyId), CancellationToken.None);

        Assert.Single(lobbies.GetAllIds());
    }
}
