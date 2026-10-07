using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Entities;

public class WordRaceTests
{
    private const string Passage = "the quick brown fox jumps over the lazy dog";

    private static (WordRace Race, RaceParticipant Participant, FakeClock Clock) CreateStartedRace(IAntiCheatPolicy policy)
    {
        var clock = new FakeClock();
        var race = new WordRace(Guid.NewGuid(), 9);
        race.SetPassage(Passage);
        var participant = new RaceParticipant(Guid.NewGuid(), "#fff", "alice", policy, clock.Func);
        race.AddParticipant(participant);
        race.Start();
        return (race, participant, clock);
    }

    [Fact]
    public void ProcessUpdate_RejectedUpdate_IsDroppedWithoutThrowing()
    {
        var (race, participant, _) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000));

        race.ProcessUpdate(participant.Id, 2, 0, "txe");

        Assert.Equal(-1, participant.Index);
        Assert.Equal("", participant.Typed);
    }

    [Fact]
    public void ProcessUpdate_RejectedUpdate_RaisesRaceViolationEventWithRule()
    {
        var (race, participant, _) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000));

        race.ProcessUpdate(participant.Id, 2, 0, "txe");

        var violation = Assert.Single(race.DomainEvents.OfType<RaceViolationEvent>());
        Assert.Equal(race.LobbyId, violation.LobbyId);
        Assert.Equal(participant.Id, violation.PlayerId);
        Assert.Equal("typed prefix does not match passage", violation.Rule);
    }

    [Fact]
    public void ProcessUpdate_ClampedUpdate_AppliesClampedState()
    {
        var policy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));
        var (race, participant, _) = CreateStartedRace(policy);

        race.ProcessUpdate(participant.Id, 8, 0, "the quick");

        Assert.Equal("the qu", participant.Typed);
        Assert.Equal(5, participant.Index);
    }

    [Fact]
    public void RetryPendingClaims_StalledFinalKeystroke_EventuallyFinishesAndRaisesPlayerFinishedEvent()
    {
        // Slack covers everything but the last character, so the final keystroke clamps and stalls.
        var policy = new FakeAntiCheatPolicy(maxCharsPerSecond: 100, budgetSlack: Passage.Length - 1);
        var (race, participant, clock) = CreateStartedRace(policy);

        race.ProcessUpdate(participant.Id, Passage.Length - 1, 0, Passage);
        Assert.False(participant.IsFinished);

        clock.Advance(TimeSpan.FromMilliseconds(100));
        race.RetryPendingClaims();

        Assert.True(participant.IsFinished);
        var finished = Assert.Single(race.DomainEvents.OfType<PlayerFinishedEvent>());
        Assert.Equal(participant.Id, finished.PlayerId);
    }
}
