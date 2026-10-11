using FasterNFaster.Api.UseCases.Lobbies.Exceptions;

namespace FasterNFaster.UnitTests.Lobbies;

public class LobbyQueryTests
{
    [Fact]
    public async Task TwoPlayers_MarksOnlyHostAsHost()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        Assert.Equal(host.Id, Assert.Single(state.Players, p => p.IsHost).Id);
        Assert.Contains(state.Players, p => p.Id == other.Id && !p.IsHost);
    }

    [Fact]
    public async Task TwoPlayers_TakenColorsMatchPlayers()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        var taken = state.Colors.Where(c => !c.IsAvailable).Select(c => c.Color);
        Assert.Equal(state.Players.Select(p => p.Color).Order(), taken.Order());
    }

    [Fact]
    public async Task LaterJoin_DoesNotChangeReturnedState()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        await context.LobbyAccess.Mutate(context.LobbyId, l => l.Join(Guid.NewGuid(), "late", null));

        Assert.Equal(2, state.Players.Count);
        Assert.Equal(2, state.Colors.Count(c => !c.IsAvailable));
    }

    [Fact]
    public async Task LobbyGoneButRaceRegistered_ThrowsLobbyNotFound()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await context.LobbyAccess.RemoveIfEmpty(context.LobbyId);

        await Assert.ThrowsAsync<LobbyNotFoundException>(() => context.LobbyQuery.GetLobbyState(context.LobbyId));
    }
}
