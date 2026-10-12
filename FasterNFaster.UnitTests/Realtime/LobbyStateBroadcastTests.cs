using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Lobbies.Colors;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.ChangeColor;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;
using FasterNFaster.Api.UseCases.Lobbies.KickPlayer;
using FasterNFaster.Api.UseCases.Lobbies.Refresh;
using FasterNFaster.Api.UseCases.Lobbies.TransferHost;
using FasterNFaster.Api.UseCases.Races.RefreshPassage;

namespace FasterNFaster.UnitTests.Realtime;

public class LobbyStateBroadcastTests
{
    [Fact]
    public async Task JoinLobby_BroadcastsLobbyStateOnce()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var joiner = new User("joiner");

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () =>
            new JoinLobbyHandler(context.Lobbies)
                .Handle(new JoinLobbyCommand(joiner.Id, context.LobbyId, joiner.Nick), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task ChangeColor_BroadcastsLobbyStateOnce()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () => ChangeColor(context, other.Id, PlayerColors.Palette[2]));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task KickPlayer_BroadcastsLobbyStateOnce()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () =>
            new KickPlayerHandler(context.Lobbies)
                .Handle(new KickPlayerCommand(host.Id, other.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task TransferHost_BroadcastsLobbyStateOnce()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () =>
            new TransferHostHandler(context.Lobbies)
                .Handle(new TransferHostCommand(host.Id, other.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task Disconnect_BroadcastsLobbyStateOnce()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () =>
            new DisconnectHandler(context.Lobbies, context.Races)
                .Handle(new DisconnectCommand(other.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task StartRace_BroadcastsLobbyStateOnce()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () => LobbyFactory.StartRace(context, host.Id));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task RefreshPassage_BroadcastsLobbyStateOnce()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () =>
            new RefreshPassageHandler(context.Lobbies, context.PassageRefresher)
                .Handle(new RefreshPassageCommand(host.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task Refresh_BroadcastsLobbyStateOnce()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () =>
            new RefreshHandler(new PendingRemovalRegistry(), context.Lobbies, context.Tracker)
                .Handle(new RefreshCommand(host.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task FailedCommand_BroadcastsNoLobbyState()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();
        Exception? exception = null;

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, async () =>
            exception = await Record.ExceptionAsync(() => ChangeColor(context, other.Id, PlayerColors.Palette[0])));

        Assert.IsType<ColorIsAlreadyTakenException>(exception);
        Assert.Equal(0, broadcasts);
    }

    private static Task ChangeColor(LobbyTestContext context, Guid callerId, string color) =>
        new ChangeColorHandler(context.Lobbies)
            .Handle(new ChangeColorCommand(callerId, color), CancellationToken.None);
}
