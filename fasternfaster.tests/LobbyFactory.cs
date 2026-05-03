using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Factories.Implementations;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests;

public record LobbyTestContext(
    LobbyStore Store,
    LobbyService LobbyService,
    RaceTickRegistry Registry,
    FakeUserRepository UserRepo,
    Guid LobbyId,
    FakeEventDispatcher EventDispatcher)
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
        var eventDispatcher = new FakeEventDispatcher();
        var lobbyStore = new LobbyStore();
        var lobbyService = new LobbyService(lobbyStore, eventDispatcher);
        var registry = new RaceTickRegistry();
        var userRepo = new FakeUserRepository();
        var passageProvider = new RandomPassageProvider();

        var createHandler = new CreateLobbyHandler(passageProvider, lobbyService);
        var result = await createHandler.Handle(new CreateLobbyCommand("Test", false, hostId));

        return new LobbyTestContext(lobbyStore, lobbyService, registry, userRepo, result.LobbyId, eventDispatcher);
    }

    /// <summary>
    /// Creates a lobby with all users joined and connections tracked.
    /// First user is the host.
    /// </summary>
    public static async Task<LobbyTestContext> WithPlayers(params User[] users)
    {
        var userRepo = new FakeUserRepository();
        var userFactory = new UserFactory(userRepo);

        var eventDispatcher = new FakeEventDispatcher();
        var lobbyStore = new LobbyStore();
        var lobbyService = new LobbyService(lobbyStore, eventDispatcher);
        var registry = new RaceTickRegistry();
        var passageProvider = new RandomPassageProvider();

        foreach (var user in users)
            userRepo.Seed(user);

        var createHandler = new CreateLobbyHandler(passageProvider, lobbyService);
        var result = await createHandler.Handle(new CreateLobbyCommand("Test", false, users[0].Id));

        var joinHandler = new JoinLobbyHandler(userFactory, lobbyService);
        for (int i = 0; i < users.Length; i++)
        {
            await joinHandler.Handle(new JoinLobbyCommand(users[i].Id, result.LobbyId, "test", "Guest"));
        }

        return new LobbyTestContext(lobbyStore, lobbyService, registry, userRepo, result.LobbyId, eventDispatcher);
    }
    public static async Task<(User host, User other, LobbyTestContext context)> TwoUsersSetup()
    {
        User host = new User("host");
        User other = new User("other");

        var context = await WithPlayers(host, other);

        return (host, other, context);
    }
}
