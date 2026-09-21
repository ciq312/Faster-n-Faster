using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Tests;

public class LobbyAccessTests
{
    [Fact]
    public async Task Mutate_ShouldPersistAndDispatchEvents()
    {
        User host = new User("hehe");
        User other = new User("bebe");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host, other);

        await context.LobbyAccess.Mutate(context.LobbyId, l => l.TransferHost(host.Id, other.Id));

        Assert.True(context.Lobby.HostId == other.Id);
        Assert.Single(context.Dispatcher.Dispatched.OfType<HostChangedEvent>());
    }

    [Fact]
    public async Task Mutate_ShouldTrackAndUntrackPlayers()
    {
        User host = new User("host");
        User other = new User("other");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host, other);

        Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(other.Id));

        await context.LobbyAccess.Mutate(context.LobbyId, l => l.Disconnect(other.Id));

        Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(other.Id));
    }

    [Fact]
    public async Task Mutate_WhenAggregateThrows_ShouldNotPersist()
    {
        User host = new User("host");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LobbyAccess.Mutate(context.LobbyId, l => l.EndSession()));

        Assert.False(context.Lobby.IsSessionActive);
    }

    [Fact]
    public async Task Mutate_ShouldReleaseGateAfterThrow()
    {
        User host = new User("host");
        User other = new User("other");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host, other);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LobbyAccess.Mutate(context.LobbyId, l => l.EndSession()));

        await context.LobbyAccess.Mutate(context.LobbyId, l => l.TransferHost(host.Id, other.Id));

        Assert.Equal(other.Id, context.Lobby.HostId);
    }

    [Fact]
    public async Task Exists_ShouldReflectRemoval()
    {
        User host = new User("host");
        LobbyTestContext context = await LobbyFactory.WithPlayers(host);

        Assert.True(context.LobbyAccess.Exists(context.LobbyId));

        await context.LobbyAccess.Remove(context.LobbyId);

        Assert.False(context.LobbyAccess.Exists(context.LobbyId));
    }
}
