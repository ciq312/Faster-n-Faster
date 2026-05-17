using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Tests.Fakes;
using Google.Apis.Logging;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace FasterNFaster.Tests.Handlers;

public class CreateLobbyHandlerTests
{
    [Fact]
    public async Task CreateLobby_ShouldReturnLobbyIdAndName()
    {
        var passageProvider = new RandomPassageProvider();
        var eventDispatcher = new FakeEventDispatcher();
        var lobbyStore = new LobbyStore();

        var lobbyService = new LobbyService(lobbyStore, new AggregateRootHelper(eventDispatcher), eventDispatcher);
        var raceService = new RaceService(new AggregateRootHelper(eventDispatcher), passageProvider, NullLogger<RaceService>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbyService, raceService);

        var hostId = Guid.NewGuid();

        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, hostId));

        var storedLobby = lobbyStore.GetRequired(result.LobbyId);

        Assert.True(storedLobby.Name == "testLobby");
        Assert.True(storedLobby.HostId == hostId);
    }

    [Fact]
    public async Task CreateLobby_ShouldStoreTheLobby()
    {
        var passageProvider = new RandomPassageProvider();
        var eventDispatcher = new FakeEventDispatcher();
        var lobbyStore = new LobbyStore();

        var lobbyService = new LobbyService(lobbyStore, new AggregateRootHelper(eventDispatcher), eventDispatcher);
        var raceService = new RaceService(new AggregateRootHelper(eventDispatcher), passageProvider, NullLogger<RaceService>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbyService, raceService);
        var lobbyId = Guid.NewGuid();

        await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, lobbyId));

        Assert.Single(lobbyStore.GetAll());
    }

}
