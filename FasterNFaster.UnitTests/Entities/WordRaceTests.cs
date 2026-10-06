using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Entities;

public class WordRaceTests
{
    private const string Passage = "the quick brown fox jumps over the lazy dog";

    private static (WordRace Race, RaceParticipant Participant) CreateStartedRace(IAntiCheatPolicy policy)
    {
        var clock = new FakeClock();
        var race = new WordRace(Guid.NewGuid(), 9);
        race.SetPassage(Passage);
        var participant = new RaceParticipant(Guid.NewGuid(), "#fff", "alice", policy, clock.Func);
        race.AddParticipant(participant);
        race.Start();
        return (race, participant);
    }

    [Fact]
    public void ProcessUpdate_RejectedUpdate_IsDroppedWithoutThrowing()
    {
        var (race, participant) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000));

        race.ProcessUpdate(participant.Id, 2, 0, "txe");

        Assert.Equal(-1, participant.Index);
        Assert.Equal("", participant.Typed);
    }

    [Fact]
    public void ProcessUpdate_ClampedUpdate_AppliesClampedState()
    {
        var policy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));
        var (race, participant) = CreateStartedRace(policy);

        // 9 characters against the default slack of 6
        race.ProcessUpdate(participant.Id, 8, 0, "the quick");

        Assert.Equal("the qu", participant.Typed);
        Assert.Equal(5, participant.Index);
    }
}
