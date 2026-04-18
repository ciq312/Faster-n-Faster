using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
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
    Guid LobbyId)
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
        var store = new LobbyStore();
        var lobbyService = new LobbyService();
        var registry = new RaceTickRegistry();
        var userRepo = new FakeUserRepository();
        var passageProvider = new RandomPassageProvider();

        var createHandler = new CreateLobbyHandler(store, passageProvider, lobbyService);
        var result = await createHandler.Handle(new CreateLobbyCommand("Test", false, hostId));

        return new LobbyTestContext(store, lobbyService, registry, userRepo, result.LobbyId);
    }

    /// <summary>
    /// Creates a lobby with all users joined and connections tracked.
    /// First user is the host.
    /// </summary>
    public static async Task<LobbyTestContext> WithPlayers(params User[] users)
    {
        var store = new LobbyStore();
        var userRepo = new FakeUserRepository();
        var userFactory = new UserFactory(userRepo);
        var lobbyService = new LobbyService();
        var registry = new RaceTickRegistry();
        var passageProvider = new RandomPassageProvider();

        foreach (var user in users)
            userRepo.Seed(user);

        var createHandler = new CreateLobbyHandler(store, passageProvider, lobbyService);
        var result = await createHandler.Handle(new CreateLobbyCommand("Test", false, users[0].Id));

        var joinHandler = new JoinLobbyHandler(store, userFactory, lobbyService);
        for (int i = 0; i < users.Length; i++)
        {
            await joinHandler.Handle(new JoinLobbyCommand(users[i].Id, result.LobbyId, "test", "Guest"));
            lobbyService.TrackConnection($"conn{i}", result.LobbyId, users[i].Id);
        }

        return new LobbyTestContext(store, lobbyService, registry, userRepo, result.LobbyId);
    }
}
