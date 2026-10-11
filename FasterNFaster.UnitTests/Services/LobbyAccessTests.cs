using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Exceptions;

namespace FasterNFaster.UnitTests.Services;

public class LobbyAccessTests
{
    [Fact]
    public async Task Mutate_ShouldPersistAndDispatchEvents()
    {
        var host = new User("hehe");
        var other = new User("bebe");
        var context = await LobbyFactory.WithPlayers(host, other);

        await context.LobbyAccess.Mutate(context.LobbyId, l => l.TransferHost(host.Id, other.Id));

        Assert.True(context.Lobby.HostId == other.Id);
        Assert.Single(context.Dispatcher.Dispatched.OfType<HostChangedEvent>());
    }

    [Fact]
    public async Task Mutate_ShouldTrackAndUntrackPlayers()
    {
        var host = new User("host");
        var other = new User("other");
        var context = await LobbyFactory.WithPlayers(host, other);

        Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(other.Id));

        await context.LobbyAccess.Mutate(context.LobbyId, l => l.Disconnect(other.Id));

        Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(other.Id));
    }

    [Fact]
    public async Task Mutate_WhenAggregateThrows_ShouldNotPersist()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LobbyAccess.Mutate(context.LobbyId, l => l.EndSession()));

        Assert.False(context.Lobby.IsSessionActive);
    }

    [Fact]
    public async Task Mutate_ShouldReleaseGateAfterThrow()
    {
        var host = new User("host");
        var other = new User("other");
        var context = await LobbyFactory.WithPlayers(host, other);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LobbyAccess.Mutate(context.LobbyId, l => l.EndSession()));

        await context.LobbyAccess.Mutate(context.LobbyId, l => l.TransferHost(host.Id, other.Id));

        Assert.Equal(other.Id, context.Lobby.HostId);
    }

    [Fact]
    public async Task Exists_ShouldReflectRemoval()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.True(context.LobbyAccess.Exists(context.LobbyId));

        await context.LobbyAccess.RemoveIfEmpty(context.LobbyId);

        Assert.False(context.LobbyAccess.Exists(context.LobbyId));
    }

    [Fact]
    public async Task RemoveIfEmpty_WhenPlayersRemain_ReturnsFalseAndKeepsLobby()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));

        Assert.False(await context.LobbyAccess.RemoveIfEmpty(context.LobbyId));

        Assert.True(context.LobbyAccess.Exists(context.LobbyId));
    }

    [Fact]
    public async Task RemoveIfEmpty_WhenAlreadyRemoved_ReturnsFalse()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.True(await context.LobbyAccess.RemoveIfEmpty(context.LobbyId));

        Assert.False(await context.LobbyAccess.RemoveIfEmpty(context.LobbyId));
    }

    [Fact]
    public async Task Read_ReturnsProjectionOfCurrentState()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var ids = await context.LobbyAccess.Read(context.LobbyId, l => l.GetPlayerIds());

        Assert.Equal([host.Id, other.Id], ids);
    }

    [Fact]
    public async Task Read_WhenLobbyMissing_ThrowsLobbyNotFound()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        await Assert.ThrowsAsync<LobbyNotFoundException>(() => context.LobbyAccess.Read(Guid.NewGuid(), l => l.Id));
    }

    [Fact]
    public async Task TryRead_WhenLobbyMissing_ReturnsDefault()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.Null(await context.LobbyAccess.TryRead(Guid.NewGuid(), l => l.Name));
        Assert.False(await context.LobbyAccess.TryRead(Guid.NewGuid(), l => l.IsEmpty()));
    }

    [Fact]
    public async Task Read_AfterMutateThrows_StillAcquiresGate()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LobbyAccess.Mutate(context.LobbyId, l => l.EndSession()));

        var count = await context.LobbyAccess
            .Read(context.LobbyId, l => l.Players.Count)
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetAllIds_ReturnsCreatedLobbyId()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.Equal([context.LobbyId], context.LobbyAccess.GetAllIds());
    }

    [Fact]
    public async Task FindIdByInviteCode_LowercaseCode_ReturnsLobbyId()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var code = context.Lobby.LobbySettings.InviteCode;

        Assert.Equal(context.LobbyId, context.LobbyAccess.FindIdByInviteCode(code.ToLowerInvariant()));
    }

    [Fact]
    public async Task FindIdByInviteCode_UnknownCode_ReturnsNull()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.Null(context.LobbyAccess.FindIdByInviteCode("NOPE"));
    }
}
