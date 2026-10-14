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

        await context.Lobbies.Mutate(context.LobbyId, l => l.Join(Guid.NewGuid(), "late", null, _ => true));

        Assert.Equal(2, state.Players.Count);
        Assert.Equal(2, state.Colors.Count(c => !c.IsAvailable));
    }

    [Fact]
    public async Task LobbyGoneButRaceRegistered_ThrowsLobbyNotFound()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await context.Lobbies.RemoveIfEmpty(context.LobbyId);

        await Assert.ThrowsAsync<LobbyNotFoundException>(() => context.LobbyQuery.GetLobbyState(context.LobbyId));
    }

    [Fact]
    public async Task BeforeStart_ReportsNeitherStartingNorStarted()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        Assert.False(state.IsRaceStarting);
        Assert.False(state.IsRaceStarted);
    }

    [Fact]
    public async Task Countdown_ReportsRaceStartingOnly()
    {
        var context = await InCountdown();

        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        Assert.True(state.IsRaceStarting);
        Assert.False(state.IsRaceStarted);
    }

    [Fact]
    public async Task RaceStarted_ReportsRaceStartedOnly()
    {
        var context = await InCountdown();
        await context.Races.Mutate(context.LobbyId, r => r.Start());

        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        Assert.False(state.IsRaceStarting);
        Assert.True(state.IsRaceStarted);
    }

    [Fact]
    public async Task SessionEndedDuringCountdown_ReportsNeitherStartingNorStarted()
    {
        var context = await InCountdown();
        await context.Lobbies.Mutate(context.LobbyId, l => l.EndSession());

        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        Assert.False(state.IsRaceStarting);
        Assert.False(state.IsRaceStarted);
    }

    [Fact]
    public async Task SessionEndedBeforeRaceReset_ReportsNeitherStartingNorStarted()
    {
        var context = await InCountdown();
        // The race and lobby reads aren't atomic: the race can be read before the finish and the lobby after EndSession.
        await context.Races.Mutate(context.LobbyId, r => r.Start());
        await context.Lobbies.Mutate(context.LobbyId, l => l.EndSession());

        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);

        Assert.False(state.IsRaceStarting);
        Assert.False(state.IsRaceStarted);
    }

    private static async Task<LobbyTestContext> InCountdown()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        return context;
    }
}
