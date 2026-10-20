using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.TransferHost;

namespace FasterNFaster.UnitTests.Lobbies;

public class TransferHostHandlerTests
{
    [Fact]
    public async Task ByHost_MakesTargetHost()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await Transfer(context, host.Id, other.Id);

        Assert.Equal(other.Id, await context.ReadLobby(l => l.HostId));
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

        Assert.Equal(host.Id, await context.ReadLobby(l => l.HostId));
    }

    [Fact]
    public async Task ToSelf_ThrowsHostTransferToSelf()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<HostTransferToSelfException>(() => Transfer(context, host.Id, host.Id));
    }

    [Fact]
    public async Task TargetNotInLobby_ThrowsPlayerNotInLobby()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<PlayerNotInLobbyException>(() => Transfer(context, host.Id, Guid.NewGuid()));

        Assert.Equal(host.Id, await context.ReadLobby(l => l.HostId));
    }

    private static Task Transfer(LobbyTestContext context, Guid hostId, Guid targetId) =>
        new TransferHostHandler(context.Lobbies)
            .Handle(new TransferHostCommand(hostId, targetId), CancellationToken.None);
}
