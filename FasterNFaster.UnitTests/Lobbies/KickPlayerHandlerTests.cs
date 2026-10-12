using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

namespace FasterNFaster.UnitTests.Lobbies;

public class KickPlayerHandlerTests
{
    [Fact]
    public async Task ByHost_RemovesUntracksAndBansTarget()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var result = await Kick(context, host.Id, other.Id);

        Assert.Equal(other.Id, result.TargetPlayerId);
        Assert.False(await context.ReadLobby(l => l.IsPlayerIn(other.Id)));
        Assert.True(await context.ReadLobby(l => l.IsPlayerBanned(other.Id)));
        Assert.Null(context.Lobbies.GetLobbyIdOfPlayer(other.Id));
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
            new JoinLobbyHandler(context.Lobbies, context.Tracker)
                .Handle(new JoinLobbyCommand(other.Id, context.LobbyId, other.Nick), CancellationToken.None));

        Assert.Null(context.Lobbies.GetLobbyIdOfPlayer(other.Id));
    }

    [Fact]
    public async Task ByNonHost_ThrowsNotHost_AndKeepsTarget()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<NotHostException>(() => Kick(context, other.Id, host.Id));

        Assert.True(await context.ReadLobby(l => l.IsPlayerIn(host.Id)));
    }

    [Fact]
    public async Task DuringRace_ThrowsLobbyInRace_AndKeepsTarget()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);

        await Assert.ThrowsAsync<LobbyInRaceException>(() => Kick(context, host.Id, other.Id));

        Assert.True(await context.ReadLobby(l => l.IsPlayerIn(other.Id)));
    }

    [Fact]
    public async Task HostTargetsSelf_ThrowsHostKickSelf_AndKeepsHost()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<HostKickSelfException>(() => Kick(context, host.Id, host.Id));

        Assert.True(await context.ReadLobby(l => l.IsPlayerIn(host.Id)));
        Assert.Equal(host.Id, await context.ReadLobby(l => l.HostId));
    }

    [Fact]
    public async Task TargetNotInLobby_ThrowsUserNotFound()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<UserNotFoundException>(() => Kick(context, host.Id, Guid.NewGuid()));
    }

    private static Task<KickPlayerResult> Kick(LobbyTestContext context, Guid callerId, Guid targetId) =>
        new KickPlayerHandler(context.Lobbies, context.Tracker)
            .Handle(new KickPlayerCommand(callerId, targetId), CancellationToken.None);
}
