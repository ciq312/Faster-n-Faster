using System.Security.Claims;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Hubs;

public class GameHubSessionTests
{
    private readonly InMemorySessionService sessions = new(new InMemoryRefreshTokenRepository());
    private readonly FakeHubCallerClients clients = new();

    private GameHub NewTab(Guid userId, ILobbyAccess lobbies, IRaceAccess races)
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("name", "player"),
        ]));

        return new GameHub(new FakeLogger<GameHub>(), lobbies, sessions, races, new FakeSender())
        {
            Context = new FakeHubCallerContext(claims),
            Clients = clients,
        };
    }

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
}
