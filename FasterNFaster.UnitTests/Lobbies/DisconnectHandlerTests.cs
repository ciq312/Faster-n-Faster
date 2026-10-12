using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;

namespace FasterNFaster.UnitTests.Lobbies;

public class DisconnectHandlerTests
{
    [Fact]
    public async Task DisconnectFromLobby_ShouldRemove()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.Races);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);

        var playerIds = await context.ReadLobby(l => l.GetPlayerIds().ToList());
        Assert.Equal(host.Id, Assert.Single(playerIds));
    }

    [Fact]
    public async Task HostDisconnectFromLobby_ShouldPromoteNextAndRemoveHost()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.Races);

        await disconnectHandler.Handle(new DisconnectCommand(host.Id), CancellationToken.None);

        var (playerIds, hostId) = await context.ReadLobby(l => (l.GetPlayerIds().ToList(), l.HostId));
        Assert.Equal(hostId, Assert.Single(playerIds));
    }

    [Fact]
    public async Task DisconnectFromLobby_WhenAlreadyDisconnected_ShouldNotThrow()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.Races);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);
        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);

        Assert.Single(await context.ReadLobby(l => l.GetPlayerIds().ToList()));
    }

    [Fact]
    public async Task SoloPlayerDisconnectMidRace_ShouldRemoveLobbyAndRace()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(context, host.Id);
        LobbyFactory.WireCleanup(context);

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.Races);

        await disconnectHandler.Handle(new DisconnectCommand(host.Id), CancellationToken.None);

        Assert.False(context.LobbyExists);
        Assert.False(context.Races.Exists(context.LobbyId));
        Assert.Empty(context.Registry.GetRacingLobbies());
    }

    [Fact]
    public async Task DisconnectMidRace_ShouldWithdrawFromRaceAndKeepLobby()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        LobbyFactory.WireCleanup(context);

        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.Races);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);

        var snapshot = await context.ReadRace(r => r.GetSnapshot());
        Assert.NotNull(snapshot);
        Assert.DoesNotContain(snapshot, p => p.PlayerId == other.Id);
        Assert.Contains(snapshot, p => p.PlayerId == host.Id);
        Assert.True(context.LobbyExists);
    }

    [Fact]
    public async Task LastActiveRacerDisconnectsMidRace_DispatchesRaceFinished()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        await context.Races.Mutate(context.LobbyId, r => r.WithdrawParticipant(host.Id));
        var disconnectHandler = new DisconnectHandler(context.LobbyAccess, context.Races);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id), CancellationToken.None);

        var finished = Assert.Single(context.Dispatcher.Dispatched.OfType<RaceFinishedEvent>());
        Assert.Equal(context.LobbyId, finished.LobbyId);
    }
}
