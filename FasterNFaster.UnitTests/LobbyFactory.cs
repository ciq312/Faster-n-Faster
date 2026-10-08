using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Lobbies.Cleanup;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;
using FasterNFaster.Api.UseCases.Lobbies.StartRace;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests;

public record LobbyTestContext(
    InMemoryLobbyRepository Store,
    LobbyAccess LobbyAccess,
    LobbyQuery LobbyQuery,
    RaceAccess RaceAccess,
    LobbyStateTracker Tracker,
    RaceTickRegistry Registry,
    FakeUserRepository UserRepo,
    Guid LobbyId,
    FakeEventDispatcher Dispatcher,
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
        var dispatcher = new FakeEventDispatcher();
        var lobbyStore = new InMemoryLobbyRepository();
        var locationRegistry = new InMemoryPlayerLocationRegistry();
        var tracker = new LobbyStateTracker();
        var lobbies = new LobbyAccess(lobbyStore, locationRegistry, dispatcher, tracker);
        var registry = new RaceTickRegistry();
        var userRepo = new FakeUserRepository();
        var passageProvider = new RandomPassageProvider();
        var races = new RaceAccess(dispatcher, passageProvider, tracker, NullLogger<RaceAccess>.Instance);

        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbies, races);
        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("Test", false, hostId), CancellationToken.None);

        var lobbyQuery = new LobbyQuery(lobbies, races);

        return new LobbyTestContext(lobbyStore, lobbies, lobbyQuery, races, tracker, registry, userRepo, result.LobbyId, dispatcher, publisher);
    }

    /// <summary>
    /// Creates a lobby with all users joined and connections tracked.
    /// First user is the host.
    /// </summary>
    public static async Task<LobbyTestContext> WithPlayers(params User[] users)
    {
        var userRepo = new FakeUserRepository();

        var publisher = new FakePublisher();
        var dispatcher = new FakeEventDispatcher();
        var lobbyStore = new InMemoryLobbyRepository();
        var locationRegistry = new InMemoryPlayerLocationRegistry();
        var tracker = new LobbyStateTracker();
        var lobbies = new LobbyAccess(lobbyStore, locationRegistry, dispatcher, tracker);
        var registry = new RaceTickRegistry();
        var passageProvider = new RandomPassageProvider();

        foreach (var user in users)
            userRepo.Seed(user);

        var races = new RaceAccess(dispatcher, passageProvider, tracker, NullLogger<RaceAccess>.Instance);
        var createLobbyHandler = new CreateLobbyHandler(passageProvider, lobbies, races);
        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("Test", false, users[0].Id), CancellationToken.None);

        var joinHandler = new JoinLobbyHandler(lobbies);
        for (var i = 0; i < users.Length; i++)
        {
            await joinHandler.Handle(new JoinLobbyCommand(users[i].Id, result.LobbyId, users[i].Nick), CancellationToken.None);
        }

        var lobbyQuery = new LobbyQuery(lobbies, races);
        return new LobbyTestContext(lobbyStore, lobbies, lobbyQuery, races, tracker, registry, userRepo, result.LobbyId, dispatcher, publisher);
    }

    public static async Task<(User host, User other, LobbyTestContext context)> TwoUsersSetup()
    {
        var host = new User("host");
        var other = new User("other");

        var context = await WithPlayers(host, other);

        return (host, other, context);
    }

    public static Task StartRace(LobbyTestContext context, Guid hostId)
    {
        var antiCheatPolicy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));
        var startRaceHandler = new StartRaceHandler(context.LobbyAccess, context.RaceAccess, context.Registry, antiCheatPolicy);
        return startRaceHandler.Handle(new StartRaceCommand(hostId), CancellationToken.None);
    }

    public static void WireCleanup(LobbyTestContext context)
    {
        var cleanup = new CleanupEmptyLobbyHandler(context.LobbyAccess, context.RaceAccess, context.Registry);
        context.Dispatcher.OnDispatch = domainEvent => domainEvent is PlayerRemovedEvent removed
            ? cleanup.Handle(new DomainEventNotification<PlayerRemovedEvent>(removed), CancellationToken.None)
            : Task.CompletedTask;
    }
}
