using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Web.LobbyState;
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
        Assert.Single(context.EventDispatcher.Dispatched);
        Assert.True(context.EventDispatcher.Dispatched[0] is HostChangedEvent);
    }
}