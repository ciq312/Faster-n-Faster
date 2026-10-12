using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.UseCases.Lobbies.Exceptions;

namespace FasterNFaster.UnitTests.Lobbies;

public class InMemoryLobbyStoreTests
{
    [Fact]
    public async Task NewLobby_ReturnsIdAndInviteCode()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        var (id, inviteCode) = await context.Lobbies.Create("Another", false, Guid.NewGuid());

        Assert.True(context.Lobbies.Exists(id));
        Assert.Equal(inviteCode, await context.Lobbies.Read(id, l => l.LobbySettings.InviteCode));
        Assert.Equal(id, context.Lobbies.FindIdByInviteCode(inviteCode));
    }

    [Fact]
    public async Task Mutate_ShouldPersistAndDispatchEvents()
    {
        var host = new User("hehe");
        var other = new User("bebe");
        var context = await LobbyFactory.WithPlayers(host, other);

        await context.Lobbies.Mutate(context.LobbyId, l => l.TransferHost(host.Id, other.Id));

        Assert.Equal(other.Id, await context.ReadLobby(l => l.HostId));
        Assert.Single(context.Dispatcher.Dispatched.OfType<HostChangedEvent>());
    }

    [Fact]
    public async Task Mutate_ShouldTrackAndUntrackPlayers()
    {
        var host = new User("host");
        var other = new User("other");
        var context = await LobbyFactory.WithPlayers(host, other);

        Assert.Equal(context.LobbyId, context.Lobbies.GetLobbyIdOfPlayer(other.Id));

        await context.Lobbies.Mutate(context.LobbyId, l => l.Disconnect(other.Id));

        Assert.Null(context.Lobbies.GetLobbyIdOfPlayer(other.Id));
    }

    [Fact]
    public async Task PlayerLeavesWhileMappedToOtherLobby_KeepsMapping()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        var (otherLobbyId, _) = await context.Lobbies.Create("Other", false, Guid.NewGuid());
        await context.Lobbies.Mutate(otherLobbyId, l => l.Join(host.Id, host.Nick, null, _ => true));

        await context.Lobbies.Mutate(context.LobbyId, l => l.Disconnect(host.Id));

        Assert.Equal(otherLobbyId, context.Lobbies.GetLobbyIdOfPlayer(host.Id));
    }

    [Fact]
    public async Task PlayerInOtherLobby_ClaimRefused()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);

        Assert.False(context.Lobbies.TryClaimPlayer(host.Id, Guid.NewGuid()));
        Assert.Equal(context.LobbyId, context.Lobbies.GetLobbyIdOfPlayer(host.Id));
    }

    [Fact]
    public async Task SameLobbyClaimedTwice_BothGranted()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var playerId = Guid.NewGuid();

        Assert.True(context.Lobbies.TryClaimPlayer(playerId, context.LobbyId));
        Assert.True(context.Lobbies.TryClaimPlayer(playerId, context.LobbyId));
    }

    [Fact]
    public async Task Mutate_WhenAggregateThrows_ShouldNotPersist()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Lobbies.Mutate(context.LobbyId, l => l.EndSession()));

        Assert.False(await context.ReadLobby(l => l.IsSessionActive));
    }

    [Fact]
    public async Task Mutate_ShouldReleaseGateAfterThrow()
    {
        var host = new User("host");
        var other = new User("other");
        var context = await LobbyFactory.WithPlayers(host, other);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Lobbies.Mutate(context.LobbyId, l => l.EndSession()));

        await context.Lobbies.Mutate(context.LobbyId, l => l.TransferHost(host.Id, other.Id));

        Assert.Equal(other.Id, await context.ReadLobby(l => l.HostId));
    }

    [Fact]
    public async Task MissingLobby_TryMutateReturnsFalse()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var mutateCalled = false;

        var mutated = await context.Lobbies.TryMutate(Guid.NewGuid(), _ => mutateCalled = true);

        Assert.False(mutated);
        Assert.False(mutateCalled);
    }

    [Fact]
    public async Task Exists_ShouldReflectRemoval()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.True(context.Lobbies.Exists(context.LobbyId));

        await context.Lobbies.RemoveIfEmpty(context.LobbyId);

        Assert.False(context.Lobbies.Exists(context.LobbyId));
    }

    [Fact]
    public async Task RemoveIfEmpty_WhenPlayersRemain_ReturnsFalseAndKeepsLobby()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));

        Assert.False(await context.Lobbies.RemoveIfEmpty(context.LobbyId));

        Assert.True(context.Lobbies.Exists(context.LobbyId));
    }

    [Fact]
    public async Task RemoveIfEmpty_WhenAlreadyRemoved_ReturnsFalse()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.True(await context.Lobbies.RemoveIfEmpty(context.LobbyId));

        Assert.False(await context.Lobbies.RemoveIfEmpty(context.LobbyId));
    }

    [Fact]
    public async Task RemovedEmptyLobby_InviteCodeForgotten()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var code = await context.ReadLobby(l => l.LobbySettings.InviteCode);

        await context.Lobbies.RemoveIfEmpty(context.LobbyId);

        Assert.Null(context.Lobbies.FindIdByInviteCode(code));
    }

    [Fact]
    public async Task Read_ReturnsProjectionOfCurrentState()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var ids = await context.Lobbies.Read(context.LobbyId, l => l.GetPlayerIds());

        Assert.Equal([host.Id, other.Id], ids);
    }

    [Fact]
    public async Task Read_WhenLobbyMissing_ThrowsLobbyNotFound()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        await Assert.ThrowsAsync<LobbyNotFoundException>(() => context.Lobbies.Read(Guid.NewGuid(), l => l.Id));
    }

    [Fact]
    public async Task TryRead_WhenLobbyMissing_ReturnsDefault()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.Null(await context.Lobbies.TryRead(Guid.NewGuid(), l => l.Name));
        Assert.False(await context.Lobbies.TryRead(Guid.NewGuid(), l => l.IsEmpty()));
    }

    [Fact]
    public async Task Read_AfterMutateThrows_StillAcquiresGate()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Lobbies.Mutate(context.LobbyId, l => l.EndSession()));

        var count = await context.Lobbies
            .Read(context.LobbyId, l => l.Players.Count)
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetAllIds_ReturnsCreatedLobbyId()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.Equal([context.LobbyId], context.Lobbies.GetAllIds());
    }

    [Fact]
    public async Task AnyCaseInviteCode_FindsLobby()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var code = await context.ReadLobby(l => l.LobbySettings.InviteCode);

        Assert.Equal(context.LobbyId, context.Lobbies.FindIdByInviteCode(code.ToLowerInvariant()));
        Assert.Equal(context.LobbyId, context.Lobbies.FindIdByInviteCode(code.ToUpperInvariant()));
    }

    [Fact]
    public async Task FindIdByInviteCode_UnknownCode_ReturnsNull()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        Assert.Null(context.Lobbies.FindIdByInviteCode("NOPE"));
    }
}
