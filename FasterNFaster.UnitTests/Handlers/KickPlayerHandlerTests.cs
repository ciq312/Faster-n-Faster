using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

namespace FasterNFaster.Tests.Handlers;

public class KickPlayerHandlerTests
{
    [Fact]
    public async Task ByHost_RemovesUntracksAndBansTarget()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var result = await Kick(context, host.Id, other.Id);

        Assert.Equal(other.Id, result.TargetPlayerId);
        Assert.False(context.Lobby.IsPlayerIn(other.Id));
        Assert.True(context.Lobby.IsPlayerBanned(other.Id));
        Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(other.Id));
    }

    [Fact]
    public async Task ByHost_DispatchesPlayerKickedEvent()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await Kick(context, host.Id, other.Id);

        var kicked = Assert.Single(context.Dispatcher.Dispatched.OfType<PlayerKickedEvent>());
        Assert.Equal(other.Id, kicked.UserId);
        Assert.Equal(context.LobbyId, kicked.LobbyId);
    }

    [Fact]
    public async Task KickedPlayerRejoins_ThrowsPlayerBannedInLobby()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await Kick(context, host.Id, other.Id);

        await Assert.ThrowsAsync<PlayerBannedInLobbyException>(() =>
            new JoinLobbyHandler(context.LobbyAccess)
                .Handle(new JoinLobbyCommand(other.Id, context.LobbyId, other.Nick), CancellationToken.None));
    }

    [Fact]
    public async Task ByNonHost_ThrowsNotHost_AndKeepsTarget()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<NotHostException>(() => Kick(context, other.Id, host.Id));

        Assert.True(context.Lobby.IsPlayerIn(host.Id));
    }

    [Fact]
    public async Task DuringRace_ThrowsLobbyInRace_AndKeepsTarget()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);

        await Assert.ThrowsAsync<LobbyInRaceException>(() => Kick(context, host.Id, other.Id));

        Assert.True(context.Lobby.IsPlayerIn(other.Id));
    }

    [Fact]
    public async Task HostTargetsSelf_ThrowsHostKickSelf_AndKeepsHost()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<HostKickSelfException>(() => Kick(context, host.Id, host.Id));

        Assert.True(context.Lobby.IsPlayerIn(host.Id));
        Assert.Equal(host.Id, context.Lobby.HostId);
    }

    [Fact]
    public async Task TargetNotInLobby_ThrowsUserNotFound()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<UserNotFoundException>(() => Kick(context, host.Id, Guid.NewGuid()));
    }

    private static Task<KickPlayerResult> Kick(LobbyTestContext context, Guid callerId, Guid targetId) =>
        new KickPlayerHandler(context.LobbyAccess)
            .Handle(new KickPlayerCommand(callerId, targetId), CancellationToken.None);
}
