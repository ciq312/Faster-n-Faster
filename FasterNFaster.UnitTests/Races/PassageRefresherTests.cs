using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Races;

public class PassageRefresherTests
{
    [Fact]
    public async Task Refresh_ReplacesPassageUsingRaceWordCount()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var wordCount = await context.ReadRace(r => r.WordCount);
        var provider = new FakePassageProvider("a fresh passage");

        await new PassageRefresher(context.Races, provider, context.Tracker).Refresh(context.LobbyId);

        Assert.Equal(wordCount, Assert.Single(provider.RequestedWordCounts));
        Assert.Equal("a fresh passage", await context.ReadRace(r => r.Passage));
    }

    [Fact]
    public async Task Refresh_MarksLobbyChanged()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var broadcaster = new FakeBroadcaster();
        var scope = new LobbyStateScope(context.Tracker, context.LobbyAccess, context.LobbyQuery, broadcaster);

        await scope.Run(() => context.PassageRefresher.Refresh(context.LobbyId));

        Assert.Single(broadcaster.Broadcasts);
    }
}
