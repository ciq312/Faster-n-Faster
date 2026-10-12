using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.Api.Presentation.Web.AntiCheat;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.UnitTests.Domain;

public class RaceTests
{
    private const string Passage = "the quick brown fox jumps over the lazy dog";

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveWordCount_Throws(int wordCount) =>
        Assert.Throws<ArgumentException>(() => new Race(Guid.NewGuid(), wordCount, Passage));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_WithEmptyPassage_Throws(string? passage) =>
        Assert.ThrowsAny<ArgumentException>(() => new Race(Guid.NewGuid(), 9, passage!));

    private static (Race Race, RaceParticipant Participant, FakeClock Clock) CreateStartedRace(IAntiCheatPolicy policy, string passage = Passage)
    {
        var (race, participant, clock) = CreateRace(policy, passage);
        race.Start();
        return (race, participant, clock);
    }

    private static (Race Race, RaceParticipant Participant, FakeClock Clock) CreateRace(IAntiCheatPolicy policy, string passage = Passage)
    {
        var clock = new FakeClock();
        var race = new Race(Guid.NewGuid(), 9, passage);
        var participant = new RaceParticipant(Guid.NewGuid(), "#fff", "alice", policy, clock.Func);
        race.AddParticipant(participant);
        return (race, participant, clock);
    }

    [Fact]
    public void RejectedUpdate_ShouldBeDroppedWithoutThrowing()
    {
        var (race, participant, _) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000));

        race.ProcessUpdate(participant.Id, 2, 0, "txe");

        Assert.Equal(-1, participant.Index);
        Assert.Equal("", participant.Typed);
    }

    [Fact]
    public void RejectedUpdate_ShouldRaiseRaceViolationEventWithRule()
    {
        var (race, participant, _) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000));

        race.ProcessUpdate(participant.Id, 2, 0, "txe");

        var violation = Assert.Single(race.DomainEvents.OfType<RaceViolationEvent>());
        Assert.Equal(race.LobbyId, violation.LobbyId);
        Assert.Equal(participant.Id, violation.PlayerId);
        Assert.Equal("typed prefix does not match passage", violation.Rule);
    }

    [Fact]
    public void ClampedUpdate_ShouldApplyClampedState()
    {
        var policy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));
        var (race, participant, _) = CreateStartedRace(policy);

        // 9 characters against the default slack of 6
        race.ProcessUpdate(participant.Id, 8, 0, "the quick");

        Assert.Equal("the qu", participant.Typed);
        Assert.Equal(5, participant.Index);
    }

    [Fact]
    public void Write9Words_ShouldBe9WPM()
    {
        var (race, participant, clock) = CreateRace(new FakeAntiCheatPolicy(1000, 1000));

        clock.Advance(TimeSpan.FromMilliseconds(3500));

        race.Start();

        clock.Advance(TimeSpan.FromSeconds(60));

        race.ProcessUpdate(participant.Id, Passage.Length - 1, 0, Passage);

        Assert.Equal(9, participant.GetWPM());
    }

    [Fact]
    public void StalledFinalKeystroke_ShouldEventuallyFinishAndRaisePlayerFinishedEvent()
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

    [Fact]
    public void TryChangePassage_BeforeEnrolment_ReplacesPassage()
    {
        var race = new Race(Guid.NewGuid(), 9, Passage);

        var changed = race.TryChangePassage("another passage");

        Assert.True(changed);
        Assert.Equal("another passage", race.Passage);
    }

    [Fact]
    public void TryChangePassage_AfterParticipantsEnrolled_ReturnsFalse()
    {
        var (race, _, _) = CreateRace(new FakeAntiCheatPolicy(1000, 1000));

        var changed = race.TryChangePassage("another passage");

        Assert.False(changed);
        Assert.Equal(Passage, race.Passage);
    }

    [Fact]
    public void TryChangePassage_AfterStart_ReturnsFalse()
    {
        var (race, _, _) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000));

        var changed = race.TryChangePassage("another passage");

        Assert.False(changed);
        Assert.Equal(Passage, race.Passage);
    }

    [Fact]
    public void TryChangePassage_AfterRaceFinished_ReplacesPassage()
    {
        var (race, participant, _) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000));
        Finish(race, participant);

        var changed = race.TryChangePassage("another passage");

        Assert.True(changed);
        Assert.Equal("another passage", race.Passage);
    }

    [Fact]
    public void PassageWithRepeatedSpace_ResultCountsEachWordOnce()
    {
        const string passage = "the  quick brown";
        var (race, participant, _) = CreateStartedRace(new FakeAntiCheatPolicy(1000, 1000), passage);

        race.ProcessUpdate(participant.Id, passage.Length - 1, 0, passage);

        Assert.Equal(3, participant.Result!.WordsTyped);
    }

    [Fact]
    public void FirstFinisher_GetsPositionOne()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        Finish(race, alice);

        Assert.Equal(1, alice.FinishPosition);
    }

    [Fact]
    public void FirstFinisher_RaisesPlayerFinishedEventWithResult()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        Finish(race, alice);

        var finished = Assert.Single(race.DomainEvents.OfType<PlayerFinishedEvent>());
        Assert.Equal(race.LobbyId, finished.LobbyId);
        Assert.Equal(alice.Id, finished.PlayerId);
        Assert.Equal(1, finished.FinishPosition);
        Assert.Equal(100, finished.Accuracy);
    }

    [Fact]
    public void SecondFinisher_GetsPositionTwo()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, bob);

        Assert.Equal(2, bob.FinishPosition);
    }

    [Fact]
    public void FinisherWhileOthersStillRacing_RaisesNoRaceFinished()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        Finish(race, alice);

        Assert.DoesNotContain(race.DomainEvents, e => e is RaceFinishedEvent);
        Assert.True(race.HasStarted);
    }

    [Fact]
    public void LastFinisher_RaisesRaceFinishedWithEveryResult()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, bob);

        var finished = Assert.Single(race.DomainEvents.OfType<RaceFinishedEvent>());
        Assert.Equal(race.LobbyId, finished.LobbyId);
        Assert.Equal(
            new int?[] { 1, 2 },
            finished.Results.Select(r => r.FinishPosition).OrderBy(position => position));
    }

    [Fact]
    public void LastFinisher_ResetsRace()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, bob);

        Assert.Empty(race.Participants);
        Assert.False(race.HasStarted);
    }

    [Fact]
    public void FinishedRacerUpdatesAgain_RaisesNoSecondPlayerFinished()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, alice);

        Assert.Single(race.DomainEvents.OfType<PlayerFinishedEvent>());
    }

    [Fact]
    public void UpdateBeforeStart_IsIgnored()
    {
        var (race, participant, _) = CreateRace(new FakeAntiCheatPolicy(1000, 1000));

        race.ProcessUpdate(participant.Id, 2, 0, "the");

        Assert.Equal(-1, participant.Index);
        Assert.Empty(race.DomainEvents);
    }

    [Fact]
    public void UnknownPlayerUpdates_ThrowsUserNotFound()
    {
        var (race, _, _, _) = CreateStartedRaceWithTwoRacers();

        Assert.Throws<UserNotFoundException>(() => race.ProcessUpdate(Guid.NewGuid(), 2, 0, "the"));
    }

    [Fact]
    public void LastUnfinishedRacerWithdraws_RaisesRaceFinishedWithFinishersOnly()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        race.WithdrawParticipant(bob.Id);

        var finished = Assert.Single(race.DomainEvents.OfType<RaceFinishedEvent>());
        var result = Assert.Single(finished.Results);
        Assert.Equal(alice.Id, result.LobbyPlayerId);
    }

    [Fact]
    public void RacerWithdrawsWhileOthersRace_RaisesNoRaceFinished()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        race.WithdrawParticipant(alice.Id);

        Assert.DoesNotContain(race.DomainEvents, e => e is RaceFinishedEvent);
        Assert.True(race.HasStarted);
    }

    [Fact]
    public void EveryoneWithdraws_RaisesRaceFinishedWithNoResults()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();

        race.WithdrawParticipant(alice.Id);
        race.WithdrawParticipant(bob.Id);

        var finished = Assert.Single(race.DomainEvents.OfType<RaceFinishedEvent>());
        Assert.Empty(finished.Results);
    }

    [Fact]
    public void FinishedRacerWithdraws_KeepsFinishTime()
    {
        var (race, alice, _, clock) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);
        var finishedAtBefore = alice.FinishedAt;
        clock.Advance(TimeSpan.FromSeconds(1));

        race.WithdrawParticipant(alice.Id);

        Assert.Equal(finishedAtBefore, alice.FinishedAt);
    }

    [Fact]
    public void UnknownPlayerWithdraws_IsNoOp()
    {
        var (race, _, _, _) = CreateStartedRaceWithTwoRacers();

        race.WithdrawParticipant(Guid.NewGuid());

        Assert.Empty(race.DomainEvents);
        Assert.True(race.HasStarted);
    }

    private static (Race Race, RaceParticipant Alice, RaceParticipant Bob, FakeClock Clock) CreateStartedRaceWithTwoRacers()
    {
        var policy = new FakeAntiCheatPolicy(1000, 1000);
        var (race, alice, clock) = CreateRace(policy);
        var bob = new RaceParticipant(Guid.NewGuid(), "#000", "bob", policy, clock.Func);
        race.AddParticipant(bob);
        race.Start();
        return (race, alice, bob, clock);
    }

    private static void Finish(Race race, RaceParticipant racer) =>
        race.ProcessUpdate(racer.Id, Passage.Length - 1, 0, Passage);
}
