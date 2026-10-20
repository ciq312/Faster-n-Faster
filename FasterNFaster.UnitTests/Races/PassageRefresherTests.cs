using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Races;

public class PassageRefresherTests
{
    [Fact]
    public async Task ExistingRace_FetchNextUsesRaceWordCount()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var wordCount = await context.ReadRace(r => r.WordCount);
        var provider = new FakePassageProvider("a fresh passage");

        var passage = await new PassageRefresher(context.Races, provider).FetchNext(context.LobbyId);

        Assert.Equal("a fresh passage", passage);
        Assert.Equal(wordCount, Assert.Single(provider.RequestedWordCounts));
    }

    [Fact]
    public async Task IdleRace_TryApplyReplacesPassage()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        var changed = await context.PassageRefresher.TryApply(context.LobbyId, "a fresh passage");

        Assert.True(changed);
        Assert.Equal("a fresh passage", await context.ReadRace(r => r.Passage));
    }

    [Fact]
    public async Task StartedRace_TryApplyReturnsFalse()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        await context.Races.Mutate(context.LobbyId, r => r.Start());
        var passageBefore = await context.ReadRace(r => r.Passage);

        var changed = await context.PassageRefresher.TryApply(context.LobbyId, "a fresh passage");

        Assert.False(changed);
        Assert.Equal(passageBefore, await context.ReadRace(r => r.Passage));
    }

    [Fact]
    public async Task RemovedRace_TryApplyReturnsFalse()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        context.Races.Remove(context.LobbyId);

        var changed = await context.PassageRefresher.TryApply(context.LobbyId, "a fresh passage");

        Assert.False(changed);
    }
}
