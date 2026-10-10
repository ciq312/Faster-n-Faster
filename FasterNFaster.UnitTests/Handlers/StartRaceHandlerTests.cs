using FasterNFaster.Api.Core.Exceptions.Lobbies;

namespace FasterNFaster.Tests.Handlers;

public class StartRaceHandlerTests
{
    [Fact]
    public async Task ByHost_AddsEveryLobbyPlayerAsParticipant()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        await LobbyFactory.StartRace(context, host.Id);

        var snapshot = await context.RaceAccess.GetSnapshotOrDefault(context.LobbyId);
        Assert.NotNull(snapshot);
        Assert.Equal(new[] { host.Id, other.Id }.ToHashSet(), snapshot.Select(p => p.PlayerId).ToHashSet());
        Assert.True(context.Lobby.IsSessionActive);
    }

    [Fact]
    public async Task ByNonHost_ThrowsNotHostAndLeavesSessionInactive()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<NotHostException>(() => LobbyFactory.StartRace(context, other.Id));

        Assert.False(context.Lobby.IsSessionActive);
    }
}
