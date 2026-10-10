using System.Security.Claims;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.FastReconnect;
using FasterNFaster.Api.UseCases.Lobbies.Refresh;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace FasterNFaster.Tests.Hubs;

public class GameHubSessionTests
{
    private readonly InMemorySessionService sessions = new(new InMemoryRefreshTokenRepository());
    private readonly FakeHubCallerClients clients = new();
    private readonly FakeSender sender = new();
    private readonly FakeTimeProvider time = new(DateTimeOffset.UtcNow);
    private readonly PendingRemovalRegistry pendingRemovals = new();

    private GameHub NewTab(Guid userId, ILobbyAccess lobbies, IRaceAccess races)
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
            DisconnectCommand command => new DisconnectHandler(context.LobbyAccess, context.RaceAccess).Handle(command, CancellationToken.None),
            RefreshCommand command => new RefreshHandler(pendingRemovals, context.LobbyAccess, context.Tracker).Handle(command, CancellationToken.None),
            _ => Task.CompletedTask,
        };

    [Fact]
    public async Task Disconnect_OutsideLobby_ClearsSession()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var guestId = Guid.NewGuid();

        var tab = NewTab(guestId, context.LobbyAccess, context.RaceAccess);
        await tab.OnConnectedAsync();
        await tab.OnDisconnectedAsync(null);

        Assert.Null(sessions.GetActiveSession(guestId));
    }

    [Fact]
    public async Task Disconnect_InLobby_ClearsSession()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);

        var tab = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tab.OnConnectedAsync();
        await tab.OnDisconnectedAsync(null);

        Assert.Null(sessions.GetActiveSession(member.Id));
    }

    [Fact]
    public async Task StaleDisconnect_InLobbyAfterNewTabConnected_KeepsNewSession()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);

        var tabA = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabA.OnConnectedAsync();

        var tabB = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabB.OnConnectedAsync();

        await tabA.OnDisconnectedAsync(null);

        Assert.Equal(tabB.Context.ConnectionId, sessions.GetActiveSession(member.Id));
    }

    [Fact]
    public async Task StaleDisconnect_OutsideLobbyAfterNewTabConnected_KeepsNewSession()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var guestId = Guid.NewGuid();

        var tabA = NewTab(guestId, context.LobbyAccess, context.RaceAccess);
        await tabA.OnConnectedAsync();

        var tabB = NewTab(guestId, context.LobbyAccess, context.RaceAccess);
        await tabB.OnConnectedAsync();

        await tabA.OnDisconnectedAsync(null);

        Assert.Equal(tabB.Context.ConnectionId, sessions.GetActiveSession(guestId));
    }

    [Fact]
    public async Task Connect_WithExistingSession_NotifiesPreviousConnectionAndStoresNew()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var guestId = Guid.NewGuid();

        var tabA = NewTab(guestId, context.LobbyAccess, context.RaceAccess);
        await tabA.OnConnectedAsync();

        var tabB = NewTab(guestId, context.LobbyAccess, context.RaceAccess);
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

        var tabA = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabA.OnConnectedAsync();
        var disconnectA = tabA.OnDisconnectedAsync(null);

        var tabB = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabB.OnConnectedAsync();
        await tabB.RefreshLobby();
        var disconnectB = tabB.OnDisconnectedAsync(null);

        var tabC = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabC.OnConnectedAsync();
        await tabC.RefreshLobby();

        time.Advance(FastReconnectHandler.ReconnectGracePeriod);
        await Task.WhenAll(disconnectA, disconnectB);

        Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(member.Id));
    }

    [Fact]
    public async Task DisconnectTwiceThenRefresh_KeepsPlayerInLobby()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);
        RouteToLobbyHandlers(context);

        var tabA = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabA.OnConnectedAsync();
        var disconnectA = tabA.OnDisconnectedAsync(null);

        var tabB = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabB.OnConnectedAsync();
        var disconnectB = tabB.OnDisconnectedAsync(null);

        var tabC = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tabC.OnConnectedAsync();
        await tabC.RefreshLobby();

        time.Advance(FastReconnectHandler.ReconnectGracePeriod);
        await Task.WhenAll(disconnectA, disconnectB);

        Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(member.Id));
    }

    [Fact]
    public async Task Disconnect_InLobbyAfterGracePeriod_RemovesPlayer()
    {
        var member = new User("member");
        var context = await LobbyFactory.WithPlayers(member);
        RouteToLobbyHandlers(context);

        var tab = NewTab(member.Id, context.LobbyAccess, context.RaceAccess);
        await tab.OnConnectedAsync();
        var disconnect = tab.OnDisconnectedAsync(null);

        time.Advance(FastReconnectHandler.ReconnectGracePeriod);
        await disconnect;

        Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(member.Id));
    }
}
