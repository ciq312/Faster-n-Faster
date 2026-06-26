using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Helpers;
using FasterNFaster.Api.UseCases.Factories.Implementations;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests;

public record LobbyTestContext(
    LobbyStore Store,
    LobbyService LobbyService,
    LobbySessionService LobbySessionService,
    RaceTickRegistry Registry,
    FakeUserRepository UserRepo,
    Guid LobbyId,
    FakePublisher Publisher)
{
    public Lobby Lobby => Store.Get(LobbyId)!;
}

public static class LobbyFactory
{
    /// <summary>
    /// Creates a lobby with no players. Host is assigned but not joined.
    /// </summary>
    public static async Task<LobbyTestContext> Empty(Guid hostId)
    {
        var publisher = new FakePublisher();
        var lobbyStore = new LobbyStore();
        var locationRegistry = new InMemoryPlayerLocationRegistry();
        var lobbyService = new LobbyService(lobbyStore, new AggregateRootHelper(publisher), publisher, locationRegistry);
        var registry = new RaceTickRegistry();
        var userRepo = new FakeUserRepository();
        var passageProvider = new RandomPassageProvider();
        var antiCheatPolicy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));
        var raceService = new RaceService(new AggregateRootHelper(publisher), passageProvider, antiCheatPolicy, NullLogger<RaceService>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbyService, raceService);
        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("Test", false, hostId), CancellationToken.None);

        var lobbySessionService = new LobbySessionService(lobbyService, raceService, lobbyService, raceService, registry);

        return new LobbyTestContext(lobbyStore, lobbyService, lobbySessionService, registry, userRepo, result.LobbyId, publisher);
    }

    /// <summary>
    /// Creates a lobby with all users joined and connections tracked.
    /// First user is the host.
    /// </summary>
    public static async Task<LobbyTestContext> WithPlayers(params User[] users)
    {
        var userRepo = new FakeUserRepository();
        var userFactory = new UserFactory(userRepo);

        var publisher = new FakePublisher();
        var lobbyStore = new LobbyStore();
        var locationRegistry = new InMemoryPlayerLocationRegistry();
        var lobbyService = new LobbyService(lobbyStore, new AggregateRootHelper(publisher), publisher, locationRegistry);
        var registry = new RaceTickRegistry();
        var passageProvider = new RandomPassageProvider();

        foreach (var user in users)
            userRepo.Seed(user);

        var antiCheatPolicy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));
        var raceService = new RaceService(new AggregateRootHelper(publisher), passageProvider, antiCheatPolicy, NullLogger<RaceService>.Instance);
        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbyService, raceService);
        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("Test", false, users[0].Id), CancellationToken.None);

        var joinHandler = new JoinLobbyHandler(userFactory, lobbyService);
        for (int i = 0; i < users.Length; i++)
        {
            await joinHandler.Handle(new JoinLobbyCommand(users[i].Id, result.LobbyId, "test", "Guest"), CancellationToken.None);
        }

        var lobbySessionService = new LobbySessionService(lobbyService, raceService, lobbyService, raceService, registry);
        return new LobbyTestContext(lobbyStore, lobbyService, lobbySessionService, registry, userRepo, result.LobbyId, publisher);
    }

    public static async Task<(User host, User other, LobbyTestContext context)> TwoUsersSetup()
    {
        User host = new User("host");
        User other = new User("other");

        var context = await WithPlayers(host, other);

        return (host, other, context);
    }
}
