using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Handlers;

public class CreateLobbyHandlerTests
{
    [Fact]
    public async Task CreateLobby_ShouldReturnLobbyIdAndName()
    {
        var passageProvider = new RandomPassageProvider();
        var publisher = new FakePublisher();
        var dispatcher = new FakeEventDispatcher();
        var lobbyStore = new InMemoryLobbyStore();

        var lobbyService = new LobbyService(lobbyStore, new AggregateRootHelper(dispatcher), dispatcher, new InMemoryPlayerLocationRegistry());
        var raceService = new RaceService(new AggregateRootHelper(dispatcher), passageProvider, new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions())), NullLogger<RaceService>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbyService, raceService);

        var hostId = Guid.NewGuid();

        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, hostId), CancellationToken.None);

        var storedLobby = lobbyStore.GetRequired(result.LobbyId);

        Assert.True(storedLobby.Name == "testLobby");
        Assert.True(storedLobby.HostId == hostId);
    }

    [Fact]
    public async Task CreateLobby_ShouldStoreTheLobby()
    {
        var passageProvider = new RandomPassageProvider();
        var publisher = new FakePublisher();
        var dispatcher = new FakeEventDispatcher();
        var lobbyStore = new InMemoryLobbyStore();

        var lobbyService = new LobbyService(lobbyStore, new AggregateRootHelper(dispatcher), dispatcher, new InMemoryPlayerLocationRegistry());
        var raceService = new RaceService(new AggregateRootHelper(dispatcher), passageProvider, new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions())), NullLogger<RaceService>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbyService, raceService);
        var lobbyId = Guid.NewGuid();

        await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, lobbyId), CancellationToken.None);

        Assert.Single(lobbyStore.GetAll());
    }
}
