using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.UnitTests.Races;

public class StartRaceHandlerTests
{
    [Fact]
    public async Task ByHost_AddsEveryLobbyPlayerAsParticipant()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await LobbyFactory.StartRace(context, host.Id);

        var snapshot = await context.ReadRace(r => r.GetSnapshot());
        Assert.NotNull(snapshot);
        Assert.Equal(new[] { host.Id, other.Id }.ToHashSet(), snapshot.Select(p => p.PlayerId).ToHashSet());
        Assert.True(await context.ReadLobby(l => l.IsSessionActive));
    }

    [Fact]
    public async Task ByNonHost_ThrowsNotHostAndLeavesSessionInactive()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<NotHostException>(() => LobbyFactory.StartRace(context, other.Id));

        Assert.False(await context.ReadLobby(l => l.IsSessionActive));
    }

    [Fact]
    public async Task ByHost_RegistersLobbyForTickInCountdown()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        await LobbyFactory.StartRace(context, host.Id);

        var entry = Assert.Single(context.Registry.GetRacingLobbies());
        Assert.Equal(context.LobbyId, entry.LobbyId);
        Assert.Equal(RacePhase.Countdown, entry.Phase);
    }

    [Fact]
    public async Task StartedTwice_ThrowsLobbyInRace_AndKeepsTickEntry()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        context.Registry.TransitionToRacing(context.LobbyId);

        await Assert.ThrowsAsync<LobbyInRaceException>(() => LobbyFactory.StartRace(context, host.Id));

        var entry = Assert.Single(context.Registry.GetRacingLobbies());
        Assert.Equal(RacePhase.Racing, entry.Phase);
    }

    [Fact]
    public async Task CallerNotInLobby_ThrowsUserNotFound()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<UserNotFoundException>(() => LobbyFactory.StartRace(context, Guid.NewGuid()));

        Assert.Empty(context.Registry.GetRacingLobbies());
    }
}
