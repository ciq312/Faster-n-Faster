using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;

namespace FasterNFaster.Tests.Handlers;

public class DisconnectHandlerTests
{
    [Fact]
    public async Task DisconnectFromLobby_ShouldRemove()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.RaceAccess);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);

        Assert.Single(context.Lobby.Players);
        Assert.True(context.Lobby.Players.ToList()[0].Id == host.Id);
    }
    [Fact]
    public async Task HostDisconnectFromLobby_ShouldPromoteNextAndRemoveHost()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.RaceAccess);

        await disconnectHandler.Handle(new DisconnectCommand(host.Id), CancellationToken.None);

        Assert.Single(context.Lobby.Players);
        Assert.True(context.Lobby.Players.ToList()[0].Id == context.Lobby.HostId);
    }

    [Fact]
    public async Task DisconnectFromLobby_WhenAlreadyDisconnected_ShouldNotThrow()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.RaceAccess);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);
        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);

        Assert.Single(context.Lobby.Players);
    }

    [Fact]
    public async Task SoloPlayerDisconnectMidRace_ShouldRemoveLobbyAndRace()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(context, host.Id);
        LobbyFactory.WireCleanup(context);

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.RaceAccess);

        await disconnectHandler.Handle(new DisconnectCommand(host.Id), CancellationToken.None);

        Assert.Null(context.Store.Get(context.LobbyId));
        Assert.Null(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
        Assert.Empty(context.Registry.GetRacingLobbies());
    }

    [Fact]
    public async Task DisconnectMidRace_ShouldWithdrawFromRaceAndKeepLobby()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        LobbyFactory.WireCleanup(context);

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.RaceAccess);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);

        var snapshot = await context.RaceAccess.GetSnapshotOrDefault(context.LobbyId);
        Assert.NotNull(snapshot);
        Assert.DoesNotContain(snapshot, p => p.PlayerId == other.Id);
        Assert.Contains(snapshot, p => p.PlayerId == host.Id);
        Assert.NotNull(context.Store.Get(context.LobbyId));
    }
}
