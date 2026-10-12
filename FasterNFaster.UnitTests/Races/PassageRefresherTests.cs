using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Races;

public class PassageRefresherTests
{
    [Fact]
    public async Task ExistingRace_ReplacesPassageUsingRaceWordCount()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var wordCount = await context.ReadRace(r => r.WordCount);
        var provider = new FakePassageProvider("a fresh passage");

        var changed = await new PassageRefresher(context.Lobbies, context.Races, provider, context.Tracker).TryRefresh(context.LobbyId);

        Assert.True(changed);
        Assert.Equal(wordCount, Assert.Single(provider.RequestedWordCounts));
        Assert.Equal("a fresh passage", await context.ReadRace(r => r.Passage));
    }

    [Fact]
    public async Task ExistingRace_MarksLobbyChanged()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, () => context.PassageRefresher.TryRefresh(context.LobbyId));

        Assert.Equal(1, broadcasts);
    }

    [Fact]
    public async Task ActiveSession_ReturnsFalseAndDoesNotMark()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        var passageBefore = await context.ReadRace(r => r.Passage);
        var changed = true;

        var broadcasts = await LobbyFactory.LobbyStateBroadcasts(context, async () =>
            changed = await context.PassageRefresher.TryRefresh(context.LobbyId));

        Assert.False(changed);
        Assert.Equal(passageBefore, await context.ReadRace(r => r.Passage));
        Assert.Equal(0, broadcasts);
    }

    [Fact]
    public async Task LobbyRemovedDuringFetch_ReturnsFalse()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var provider = new GatedPassageProvider();
        var refresh = new PassageRefresher(context.Lobbies, context.Races, provider, context.Tracker).TryRefresh(context.LobbyId);
        await provider.WaitForRequest();

        await LobbyFactory.Remover(context).TryRemove(context.LobbyId);
        provider.Complete("fresh words");

        Assert.False(await refresh);
    }
}
