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
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Realtime;

public class LobbyStateBroadcastTests
{
    [Fact]
    public async Task JoinLobby_BroadcastsLobbyStateOnce()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var joiner = new User("joiner");

        var broadcasts = await LobbyStateBroadcasts(context, () =>
            new JoinLobbyHandler(context.LobbyAccess)
                .Handle(new JoinLobbyCommand(joiner.Id, context.LobbyId, joiner.Nick), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task ChangeColor_BroadcastsLobbyStateOnce()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyStateBroadcasts(context, () => ChangeColor(context, other.Id, PlayerColors.Palette[2]));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task KickPlayer_BroadcastsLobbyStateOnce()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyStateBroadcasts(context, () =>
            new KickPlayerHandler(context.LobbyAccess)
                .Handle(new KickPlayerCommand(host.Id, other.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task TransferHost_BroadcastsLobbyStateOnce()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyStateBroadcasts(context, () =>
            new TransferHostHandler(context.LobbyAccess)
                .Handle(new TransferHostCommand(host.Id, other.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task Disconnect_BroadcastsLobbyStateOnce()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyStateBroadcasts(context, () =>
            new DisconnectHandler(context.LobbyAccess, context.Races)
                .Handle(new DisconnectCommand(other.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task StartRace_BroadcastsLobbyStateOnce()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyStateBroadcasts(context, () => LobbyFactory.StartRace(context, host.Id));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task RefreshPassage_BroadcastsLobbyStateOnce()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyStateBroadcasts(context, () =>
            new RefreshPassageHandler(context.LobbyAccess, context.PassageRefresher)
                .Handle(new RefreshPassageCommand(host.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task Refresh_BroadcastsLobbyStateOnce()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var broadcasts = await LobbyStateBroadcasts(context, () =>
            new RefreshHandler(new PendingRemovalRegistry(), context.LobbyAccess, context.Tracker)
                .Handle(new RefreshCommand(host.Id), CancellationToken.None));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task FailedCommand_BroadcastsNoLobbyState()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();
        Exception? exception = null;

        var broadcasts = await LobbyStateBroadcasts(context, async () =>
            exception = await Record.ExceptionAsync(() => ChangeColor(context, other.Id, PlayerColors.Palette[0])));

        Assert.IsType<ColorIsAlreadyTakenException>(exception);
        Assert.Equal(0, broadcasts);
    }

    private static Task ChangeColor(LobbyTestContext context, Guid callerId, string color) =>
        new ChangeColorHandler(context.LobbyAccess)
            .Handle(new ChangeColorCommand(callerId, color), CancellationToken.None);

    private static async Task<int> LobbyStateBroadcasts(LobbyTestContext context, Func<Task> command)
    {
        var broadcaster = new FakeBroadcaster();
        var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, broadcaster);

        await scope.Run(command);

        return broadcaster.Broadcasts.Count(b => b.EventName == GameEvents.LobbyState);
    }
}
