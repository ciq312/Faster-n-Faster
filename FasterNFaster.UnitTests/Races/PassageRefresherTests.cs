using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Races;

public class PassageRefresherTests
{
    [Fact]
    public async Task TryRefresh_ReplacesPassageUsingRaceWordCount()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var wordCount = await context.ReadRace(r => r.WordCount);
        var provider = new FakePassageProvider("a fresh passage");

        var changed = await new PassageRefresher(context.Races, provider, context.Tracker).TryRefresh(context.LobbyId);

        Assert.True(changed);
        Assert.Equal(wordCount, Assert.Single(provider.RequestedWordCounts));
        Assert.Equal("a fresh passage", await context.ReadRace(r => r.Passage));
    }

    [Fact]
    public async Task TryRefresh_MarksLobbyChanged()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        var broadcasts = await Broadcasts(context);

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task TryRefresh_WhenParticipantsEnrolled_ReturnsFalseAndDoesNotMark()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        var passageBefore = await context.ReadRace(r => r.Passage);
        var broadcaster = new FakeBroadcaster();
        var scope = new LobbyStateScope(context.Tracker, context.Lobbies, context.LobbyQuery, broadcaster);
        var changed = true;

        await scope.Run(async () => changed = await context.PassageRefresher.TryRefresh(context.LobbyId));

        Assert.False(changed);
        Assert.Equal(passageBefore, await context.ReadRace(r => r.Passage));
        Assert.Empty(broadcaster.Broadcasts);
    }

    private static async Task<int> Broadcasts(LobbyTestContext context)
    {
        var broadcaster = new FakeBroadcaster();
        var scope = new LobbyStateScope(context.Tracker, context.Lobbies, context.LobbyQuery, broadcaster);

        await scope.Run(() => context.PassageRefresher.TryRefresh(context.LobbyId));

        return broadcaster.Broadcasts.Count;
    }
}
