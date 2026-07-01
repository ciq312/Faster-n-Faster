using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Tests;

public class LobbyServiceTests
{
    [Fact]
    public async Task TransferHost_ShouldGiveHost()
    {
        User host = new User("hehe");
        User other = new User("bebe");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host, other);

        await context.LobbyService.TransferHost(host.Id, other.Id);

        Assert.True(context.Lobby.HostId == other.Id);
        Assert.Single(context.Dispatcher.Dispatched.OfType<HostChangedEvent>());
    }

    [Fact]
    public async Task EndSession_ShouldDeactivateSession()
    {
        User host = new User("host");
        User other = new User("other");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host, other);

        await context.LobbyService.StartSession(context.LobbyId, host.Id);
        Assert.True(context.Lobby.IsSessionActive);

        await context.LobbyService.EndSession(context.LobbyId);

        Assert.False(context.Lobby.IsSessionActive);
    }

    [Fact]
    public async Task EndSession_WhenNotActive_ShouldThrow()
    {
        User host = new User("host");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LobbyService.EndSession(context.LobbyId));
    }
}