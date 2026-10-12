using System.Security.Claims;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Presentation.Web.Realtime;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Refresh;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace FasterNFaster.UnitTests.Realtime;

public class GameHubSessionTests
{
    private readonly InMemorySessionService sessions = new(new FakeRefreshTokenRepository());
    private readonly FakeHubCallerClients clients = new();
    private readonly FakeSender sender = new();
    private readonly FakeTimeProvider time = new(DateTimeOffset.UtcNow);
    private readonly PendingRemovalRegistry pendingRemovals = new();

    private GameHub NewTab(Guid userId, ILobbyStore lobbies, IRaceStore races)
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("name", "player"),
        ]));

        return new GameHub(new FakeLogger<GameHub>(), lobbies, sessions, races, sender)
        {
            Context = new FakeHubCallerContext(claims),
            Clients = clients,
        };
    }

    private void RouteToLobbyHandlers(LobbyTestContext context) =>
        sender.OnSend = request => request switch
        {
            FastReconnectCommand command => new FastReconnectHandler(pendingRemovals, time).Handle(command, CancellationToken.None),
            DisconnectCommand command => new DisconnectHandler(context.Lobbies, context.Races).Handle(command, CancellationToken.None),
            RefreshCommand command => new RefreshHandler(pendingRemovals, context.Lobbies, context.Tracker).Handle(command, CancellationToken.None),
            _ => Task.CompletedTask,
        };

    [Fact]
    public async Task Disconnect_OutsideLobby_ClearsSession()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var guestId = Guid.NewGuid();

        var tab = NewTab(guestId, context.Lobbies, context.Races);
        await tab.OnConnectedAsync();
        await tab.OnDisconnectedAsync(null);

        Assert.Null(sessions.GetActiveSession(guestId));
    }

    [Fact]
    public async Task Disconnect_InLobby_ClearsSession()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);

        var tab = NewTab(member.Id, context.Lobbies, context.Races);
        await tab.OnConnectedAsync();
        await tab.OnDisconnectedAsync(null);

        Assert.Null(sessions.GetActiveSession(member.Id));
    }

    [Fact]
    public async Task StartedRace_UpdateAdvancesParticipant()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(context, host.Id);
        await context.Races.Mutate(context.LobbyId, r => r.Start());
        var passage = await context.ReadRace(r => r.Passage);

        await NewTab(host.Id, context.Lobbies, context.Races).UpdateRaceState(0, 0, passage[..1]);

        var index = await context.ReadRace(r => r.Participants[host.Id].Index);
        Assert.Equal(0, index);
    }

    [Fact]
    public async Task RemovedRace_UpdateDoesNotThrow()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        context.Races.Remove(context.LobbyId);

        var exception = await Record.ExceptionAsync(() =>
            NewTab(host.Id, context.Lobbies, context.Races).UpdateRaceState(0, 0, "a"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PlayerOutsideLobby_UpdateDoesNothing()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(context, host.Id);
        await context.Races.Mutate(context.LobbyId, r => r.Start());

        var exception = await Record.ExceptionAsync(() =>
            NewTab(Guid.NewGuid(), context.Lobbies, context.Races).UpdateRaceState(0, 0, "a"));

        Assert.Null(exception);
        Assert.Equal(-1, await context.ReadRace(r => r.Participants[host.Id].Index));
    }

    [Fact]
    public async Task StaleDisconnect_InLobbyAfterNewTabConnected_KeepsNewSession()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);

        var tabA = NewTab(member.Id, context.Lobbies, context.Races);
        await tabA.OnConnectedAsync();

        var tabB = NewTab(member.Id, context.Lobbies, context.Races);
        await tabB.OnConnectedAsync();

        await tabA.OnDisconnectedAsync(null);

        Assert.Equal(tabB.Context.ConnectionId, sessions.GetActiveSession(member.Id));
    }

    [Fact]
    public async Task StaleDisconnect_OutsideLobbyAfterNewTabConnected_KeepsNewSession()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var guestId = Guid.NewGuid();

        var tabA = NewTab(guestId, context.Lobbies, context.Races);
        await tabA.OnConnectedAsync();

        var tabB = NewTab(guestId, context.Lobbies, context.Races);
        await tabB.OnConnectedAsync();

        await tabA.OnDisconnectedAsync(null);

        Assert.Equal(tabB.Context.ConnectionId, sessions.GetActiveSession(guestId));
    }

    [Fact]
    public async Task Connect_WithExistingSession_NotifiesPreviousConnectionAndStoresNew()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var guestId = Guid.NewGuid();

        var tabA = NewTab(guestId, context.Lobbies, context.Races);
        await tabA.OnConnectedAsync();

        var tabB = NewTab(guestId, context.Lobbies, context.Races);
        await tabB.OnConnectedAsync();

        Assert.Contains((tabA.Context.ConnectionId, GameEvents.AnotherSessionStarted), clients.Sent);
        Assert.Equal(tabB.Context.ConnectionId, sessions.GetActiveSession(guestId));
    }

    [Fact]
    public async Task RefreshDisconnectRefresh_KeepsPlayerInLobby()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);
        RouteToLobbyHandlers(context);

        var tabA = NewTab(member.Id, context.Lobbies, context.Races);
        await tabA.OnConnectedAsync();
        var disconnectA = tabA.OnDisconnectedAsync(null);

        var tabB = NewTab(member.Id, context.Lobbies, context.Races);
        await tabB.OnConnectedAsync();
        await tabB.RefreshLobby();
        var disconnectB = tabB.OnDisconnectedAsync(null);

        var tabC = NewTab(member.Id, context.Lobbies, context.Races);
        await tabC.OnConnectedAsync();
        await tabC.RefreshLobby();

        time.Advance(FastReconnectHandler.ReconnectGracePeriod);
        await Task.WhenAll(disconnectA, disconnectB);

        Assert.Equal(context.LobbyId, context.Lobbies.GetLobbyIdOfPlayer(member.Id));
    }

    [Fact]
    public async Task DisconnectTwiceThenRefresh_KeepsPlayerInLobby()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);
        RouteToLobbyHandlers(context);

        var tabA = NewTab(member.Id, context.Lobbies, context.Races);
        await tabA.OnConnectedAsync();
        var disconnectA = tabA.OnDisconnectedAsync(null);

        var tabB = NewTab(member.Id, context.Lobbies, context.Races);
        await tabB.OnConnectedAsync();
        var disconnectB = tabB.OnDisconnectedAsync(null);

        var tabC = NewTab(member.Id, context.Lobbies, context.Races);
        await tabC.OnConnectedAsync();
        await tabC.RefreshLobby();

        time.Advance(FastReconnectHandler.ReconnectGracePeriod);
        await Task.WhenAll(disconnectA, disconnectB);

        Assert.Equal(context.LobbyId, context.Lobbies.GetLobbyIdOfPlayer(member.Id));
    }

    [Fact]
    public async Task Disconnect_InLobbyAfterGracePeriod_RemovesPlayer()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);
        RouteToLobbyHandlers(context);

        var tab = NewTab(member.Id, context.Lobbies, context.Races);
        await tab.OnConnectedAsync();
        var disconnect = tab.OnDisconnectedAsync(null);

        time.Advance(FastReconnectHandler.ReconnectGracePeriod);
        await disconnect;

        Assert.Null(context.Lobbies.GetLobbyIdOfPlayer(member.Id));
    }
}
