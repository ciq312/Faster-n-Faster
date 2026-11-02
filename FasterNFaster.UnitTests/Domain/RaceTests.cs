using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.Domain.Exceptions.Races;
using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.UnitTests.Shared;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace FasterNFaster.UnitTests.Domain;

public class RaceTests
{
    private const string Passage = "the quick brown fox jumps over the lazy dog";

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveWordCount_ConstructorShouldThrow(int wordCount) =>
        Should.Throw<ArgumentException>(() => new Race(Guid.NewGuid(), wordCount, Passage, new FakeTimeProvider()));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyPassage_ConstructorShouldThrow(string? passage) =>
        Should.Throw<ArgumentException>(() => new Race(Guid.NewGuid(), 9, passage!, new FakeTimeProvider()));

    private static (Race Race, RaceParticipant Participant, FakeTimeProvider Clock) CreateStartedRace(IAntiCheatPolicy? policy = null, string passage = Passage)
    {
        var (race, participant, clock) = CreateRace(policy, passage);
        race.Start();
        return (race, participant, clock);
    }

    private static (Race Race, RaceParticipant Participant, FakeTimeProvider Clock) CreateRace(IAntiCheatPolicy? policy = null, string passage = Passage)
    {
        var clock = new FakeTimeProvider();
        var race = new Race(Guid.NewGuid(), 9, passage, clock);
        var participant = new RaceParticipant(Guid.NewGuid(), "#fff", "alice", policy ?? AntiCheatPolicy.Unlimited, clock);
        race.AddParticipant(participant);
        return (race, participant, clock);
    }

    private static (Race Race, RaceParticipant Alice, RaceParticipant Bob, FakeTimeProvider Clock) CreateStartedRaceWithTwoRacers()
    {
        var (race, alice, clock) = CreateRace();
        var bob = new RaceParticipant(Guid.NewGuid(), "#000", "bob", AntiCheatPolicy.Unlimited, clock);
        race.AddParticipant(bob);
        race.Start();
        return (race, alice, bob, clock);
    }

    private static void Finish(Race race, RaceParticipant racer) =>
        race.ProcessUpdate(racer.Id, KeystrokeHelper.Sequence(race.Passage));

    [Fact]
    public void RejectedUpdate_ShouldBeDroppedWithoutThrowing()
    {
        var (race, participant, _) = CreateStartedRace();

        race.ProcessUpdate(participant.Id, [KeystrokeHelper.Backspace()]);

        participant.LastValidIndex.ShouldBe(-1);
        participant.WrongTail.ShouldBeEmpty();
    }

    [Fact]
    public void RejectedUpdate_ShouldRaiseRaceViolationEventWithRule()
    {
        var (race, participant, _) = CreateStartedRace();

        race.ProcessUpdate(participant.Id, [KeystrokeHelper.Backspace()]);

        var violation = race.DomainEvents.OfType<RaceViolationEvent>().ShouldHaveSingleItem();
        violation.LobbyId.ShouldBe(race.LobbyId);
        violation.PlayerId.ShouldBe(participant.Id);
        violation.Rule.ShouldBe("backspace on empty text");
    }

    [Fact]
    public void ClampedUpdate_ShouldApplyClampedState()
    {
        var (race, participant, _) = CreateStartedRace(AntiCheatPolicy.WithBudget(charsPerSecond: 10, slack: 6));

        race.ProcessUpdate(participant.Id, KeystrokeHelper.Sequence("the quick"));

        participant.LastValidIndex.ShouldBe(5);
    }

    [Fact]
    public void Write9Words_ShouldBe9WPM()
    {
        var (race, participant, clock) = CreateRace();
        clock.Advance(TimeSpan.FromMilliseconds(3500));
        race.Start();
        clock.Advance(TimeSpan.FromSeconds(60));

        race.ProcessUpdate(participant.Id, KeystrokeHelper.Sequence(Passage));

        participant.GetWPM().ShouldBe(9);
    }

    [Fact]
    public void StalledFinalKeystroke_ShouldEventuallyFinishAndRaisePlayerFinishedEvent()
    {
        var (race, participant, clock) = CreateStartedRace(AntiCheatPolicy.WithBudget(charsPerSecond: 100, slack: Passage.Length - 1));
        race.ProcessUpdate(participant.Id, KeystrokeHelper.Sequence(Passage));
        participant.IsFinished.ShouldBeFalse();

        clock.Advance(TimeSpan.FromMilliseconds(100));
        race.RetryPendingClaims();

        participant.IsFinished.ShouldBeTrue();
        race.DomainEvents.OfType<PlayerFinishedEvent>().ShouldHaveSingleItem().PlayerId.ShouldBe(participant.Id);
    }

    [Fact]
    public void Start_StampsStartTimeFromClock()
    {
        var (race, _, clock) = CreateRace();
        clock.Advance(TimeSpan.FromSeconds(42));

        race.Start();

        race.StartTime.ShouldBe(clock.GetUtcNow());
    }

    [Fact]
    public void LastRacerFinishes_StampsEndTimeFromClock()
    {
        var (race, alice, bob, clock) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);
        clock.Advance(TimeSpan.FromSeconds(10));

        Finish(race, bob);

        race.EndTime.ShouldBe(clock.GetUtcNow());
    }

    [Fact]
    public void BeforeEnrolment_PassageReplaced()
    {
        var race = new Race(Guid.NewGuid(), 9, Passage, new FakeTimeProvider());

        var changed = race.TryChangePassage("another passage");

        changed.ShouldBeTrue();
        race.Passage.ShouldBe("another passage");
    }

    [Fact]
    public void EnrolledButNotStarted_PassageReplaced()
    {
        var (race, _, _) = CreateRace();

        var changed = race.TryChangePassage("another passage");

        changed.ShouldBeTrue();
        race.Passage.ShouldBe("another passage");
    }

    [Fact]
    public void StartedRace_PassageChangeRefused()
    {
        var (race, _, _) = CreateStartedRace();

        var changed = race.TryChangePassage("another passage");

        changed.ShouldBeFalse();
        race.Passage.ShouldBe(Passage);
    }

    [Fact]
    public void FinishedRace_PassageReplaced()
    {
        var (race, participant, _) = CreateStartedRace();
        Finish(race, participant);

        var changed = race.TryChangePassage("another passage");

        changed.ShouldBeTrue();
        race.Passage.ShouldBe("another passage");
    }

    [Fact]
    public void PassageWithRepeatedSpace_ResultCountsEachWordOnce()
    {
        const string passage = "the  quick brown";
        var (race, participant, _) = CreateStartedRace(passage: passage);

        race.ProcessUpdate(participant.Id, KeystrokeHelper.Sequence(passage));

        participant.Result!.WordsTyped.ShouldBe(3);
    }

    [Fact]
    public void FirstFinisher_GetsPositionOne()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        Finish(race, alice);

        alice.FinishPosition.ShouldBe(1);
    }

    [Fact]
    public void FirstFinisher_RaisesPlayerFinishedEventWithResult()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        Finish(race, alice);

        var finished = race.DomainEvents.OfType<PlayerFinishedEvent>().ShouldHaveSingleItem();
        finished.LobbyId.ShouldBe(race.LobbyId);
        finished.PlayerId.ShouldBe(alice.Id);
        finished.FinishPosition.ShouldBe(1);
        finished.Accuracy.ShouldBe(100);
    }

    [Fact]
    public void SecondFinisher_GetsPositionTwo()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, bob);

        bob.FinishPosition.ShouldBe(2);
    }

    [Fact]
    public void FinisherWhileOthersStillRacing_RaisesNoRaceFinished()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        Finish(race, alice);

        race.DomainEvents.OfType<RaceFinishedEvent>().ShouldBeEmpty();
        race.HasStarted.ShouldBeTrue();
    }

    [Fact]
    public void LastFinisher_RaisesRaceFinishedWithEveryResult()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, bob);

        var finished = race.DomainEvents.OfType<RaceFinishedEvent>().ShouldHaveSingleItem();
        finished.LobbyId.ShouldBe(race.LobbyId);
        finished.Results.Select(r => r.FinishPosition).OrderBy(position => position).ShouldBe(new int?[] { 1, 2 });
    }

    [Fact]
    public void LastFinisher_ResetsRace()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, bob);

        race.Participants.ShouldBeEmpty();
        race.HasStarted.ShouldBeFalse();
    }

    [Fact]
    public void FinishedRacerUpdatesAgain_RaisesNoSecondPlayerFinished()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        Finish(race, alice);

        race.DomainEvents.OfType<PlayerFinishedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void UpdateBeforeStart_IsIgnored()
    {
        var (race, participant, _) = CreateRace();

        race.ProcessUpdate(participant.Id, KeystrokeHelper.Sequence("the"));

        participant.LastValidIndex.ShouldBe(-1);
        race.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UnknownPlayerUpdates_ThrowsPlayerNotInRace()
    {
        var (race, _, _, _) = CreateStartedRaceWithTwoRacers();

        Should.Throw<PlayerNotInRaceException>(() => race.ProcessUpdate(Guid.NewGuid(), KeystrokeHelper.Sequence("the")));
    }

    [Fact]
    public void LastUnfinishedRacerWithdraws_RaisesRaceFinishedWithFinishersOnly()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);

        race.WithdrawParticipant(bob.Id);

        var finished = race.DomainEvents.OfType<RaceFinishedEvent>().ShouldHaveSingleItem();
        finished.Results.ShouldHaveSingleItem().LobbyPlayerId.ShouldBe(alice.Id);
    }

    [Fact]
    public void RacerWithdrawsWhileOthersRace_RaisesNoRaceFinished()
    {
        var (race, alice, _, _) = CreateStartedRaceWithTwoRacers();

        race.WithdrawParticipant(alice.Id);

        race.DomainEvents.OfType<RaceFinishedEvent>().ShouldBeEmpty();
        race.HasStarted.ShouldBeTrue();
    }

    [Fact]
    public void EveryoneWithdraws_RaisesRaceFinishedWithNoResults()
    {
        var (race, alice, bob, _) = CreateStartedRaceWithTwoRacers();

        race.WithdrawParticipant(alice.Id);
        race.WithdrawParticipant(bob.Id);

        race.DomainEvents.OfType<RaceFinishedEvent>().ShouldHaveSingleItem().Results.ShouldBeEmpty();
    }

    [Fact]
    public void FinishedRacerWithdraws_KeepsFinishTime()
    {
        var (race, alice, _, clock) = CreateStartedRaceWithTwoRacers();
        Finish(race, alice);
        var finishedAtBefore = alice.FinishedAt;
        clock.Advance(TimeSpan.FromSeconds(1));

        race.WithdrawParticipant(alice.Id);

        alice.FinishedAt.ShouldBe(finishedAtBefore);
    }

    [Fact]
    public void UnknownPlayerWithdraws_IsNoOp()
    {
        var (race, _, _, _) = CreateStartedRaceWithTwoRacers();

        race.WithdrawParticipant(Guid.NewGuid());

        race.DomainEvents.ShouldBeEmpty();
        race.HasStarted.ShouldBeTrue();
    }
}
