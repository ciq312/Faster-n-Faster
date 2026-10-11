using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.TransferHost;

namespace FasterNFaster.UnitTests.Handlers;

public class TransferHostHandlerTests
{
    [Fact]
    public async Task ByHost_MakesTargetHost()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await Transfer(context, host.Id, other.Id);

        Assert.Equal(other.Id, context.Lobby.HostId);
    }

    [Fact]
    public async Task ByHost_DispatchesHostChangedEvent()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await Transfer(context, host.Id, other.Id);

        var changed = Assert.Single(context.Dispatcher.Dispatched.OfType<HostChangedEvent>());
        Assert.Equal(other.Id, changed.NewHostId);
        Assert.Equal(context.LobbyId, changed.LobbyId);
    }

    [Fact]
    public async Task ByNonHost_ThrowsNotHost_AndKeepsHost()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<NotHostException>(() => Transfer(context, other.Id, other.Id));

        Assert.Equal(host.Id, context.Lobby.HostId);
    }

    [Fact]
    public async Task ToSelf_ThrowsHostTransferToSelf()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<HostTransferToSelfException>(() => Transfer(context, host.Id, host.Id));
    }

    [Fact]
    public async Task TargetNotInLobby_ThrowsUserNotFound()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<UserNotFoundException>(() => Transfer(context, host.Id, Guid.NewGuid()));

        Assert.Equal(host.Id, context.Lobby.HostId);
    }

    private static Task Transfer(LobbyTestContext context, Guid hostId, Guid targetId) =>
        new TransferHostHandler(context.LobbyAccess)
            .Handle(new TransferHostCommand(hostId, targetId), CancellationToken.None);
}
