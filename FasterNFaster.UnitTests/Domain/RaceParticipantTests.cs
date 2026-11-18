using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.UnitTests.Shared;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace FasterNFaster.UnitTests.Domain;

public class RaceParticipantTests
{
    private const string Passage = "the quick brown fox jumps over the lazy dog pepe lolo gege roro gsgs asdge eergwegro oiwernoiewnviown weoriweoieoif woerignwoeig";

    private static (RaceParticipant Participant, FakeTimeProvider Clock) CreateParticipant(IAntiCheatPolicy? policy = null)
    {
        var clock = new FakeTimeProvider();
        var participant = new RaceParticipant(Guid.NewGuid(), "#fff", "alice", policy ?? AntiCheatPolicy.Unlimited, clock);
        return (participant, clock);
    }

    private static (RaceParticipant Participant, FakeTimeProvider Clock) CreateParticipantWithBudget(int slack, int maxPendingKeystrokes = 1000) =>
        CreateParticipant(AntiCheatPolicy.WithBudget(charsPerSecond: 10, slack: slack, maxPendingKeystrokes: maxPendingKeystrokes));

    [Fact]
    public void AcceptsValidProgress_ShouldUpdateFields()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("the"), Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(2);
        participant.Mistakes.ShouldBe(0);
        participant.WordsTyped.ShouldBe(1);
    }

    [Fact]
    public void AlreadyFinished_ShouldBeNoOp()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("the"), Passage);
        participant.MarkFinished(1);

        participant.UpdateProgress(KeystrokeHelper.Sequence(" q"), Passage);

        participant.LastValidIndex.ShouldBe(2);
    }

    [Fact]
    public void WrongCharacter_ShouldIncrementMistakes()
    {
        var (participant, _) = CreateParticipant();

        participant.UpdateProgress(KeystrokeHelper.Sequence("thx"), Passage);

        participant.Mistakes.ShouldBe(1);
        participant.LastValidIndex.ShouldBe(1);
        participant.WrongTail.ShouldBe("x");
    }

    [Fact]
    public void MatchingCharacterAfterWrongOne_ShouldNotCountAsMistake()
    {
        var (participant, _) = CreateParticipant();

        participant.UpdateProgress(KeystrokeHelper.Sequence("txe"), Passage);

        participant.Mistakes.ShouldBe(1);
        participant.WrongTail.ShouldBe("xe");
    }

    [Fact]
    public void BackspaceWithWrongTail_ShouldRemoveLastWrongCharacter()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("thxy"), Passage);

        var outcome = participant.UpdateProgress([KeystrokeHelper.Backspace()], Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.WrongTail.ShouldBe("x");
        participant.LastValidIndex.ShouldBe(1);
    }

    [Fact]
    public void BackspaceWithoutWrongTail_ShouldStepBackLastValidIndex()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);

        var outcome = participant.UpdateProgress([KeystrokeHelper.Backspace()], Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(3);
        participant.WordsTyped.ShouldBe(1);
    }

    [Fact]
    public void BackspaceOnEmptyText_ShouldBeRejected()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress([KeystrokeHelper.Backspace()], Passage);

        outcome.ShouldBe(ProgressOutcome.Rejected("backspace on empty text"));
        participant.LastValidIndex.ShouldBe(-1);
    }

    [Fact]
    public void OverflowLimitExceeded_ShouldBeRejected()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("the"), Passage);

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence(new string('x', RaceParticipant.MaxOverflow + 1)), Passage);

        outcome.ShouldBe(ProgressOutcome.Rejected("overflow exceeds limit"));
    }

    [Fact]
    public void OverflowLimitNotExceeded_ShouldBeAccepted()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("the"), Passage);
        var tail = new string('x', RaceParticipant.MaxOverflow);

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence(tail), Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(2);
        participant.WrongTail.ShouldBe(tail);
    }

    [Fact]
    public void TypingPastPassageEnd_ShouldBeRejected()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("axy"), "ab");

        outcome.ShouldBe(ProgressOutcome.Rejected("typed past passage end"));
        participant.Mistakes.ShouldBe(1);
    }

    [Fact]
    public void SpaceAfterCorrectCharacter_ShouldBeAccepted()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("the "), Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(3);
    }

    [Fact]
    public void SpaceAfterWrongCharacter_ShouldBeRejected()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("thex"), Passage);

        var outcome = participant.UpdateProgress([KeystrokeHelper.CharTyped(' ')], Passage);

        outcome.ShouldBe(ProgressOutcome.Rejected("space typed after wrong character"));
    }

    [Fact]
    public void KeystrokesAfterViolation_ShouldBeDropped()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("thx e"), Passage);

        participant.RetryPendingClaim(Passage);

        participant.WrongTail.ShouldBe("x");
    }

    [Fact]
    public void FinishingKeystrokesThenBackspace_ShouldNotRegisterBackspace()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress([.. KeystrokeHelper.Sequence(Passage), KeystrokeHelper.Backspace()], Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(Passage.Length - 1);
    }

    [Fact]
    public void KeystrokesBeyondBudget_ShouldBeClampedToBudget()
    {
        var (participant, _) = CreateParticipantWithBudget(3);

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);

        outcome.ShouldBe(ProgressOutcome.Clamped);
        participant.LastValidIndex.ShouldBe(2);
    }

    [Fact]
    public void RetryPendingClaim_BeforeBudgetRefills_ShouldStayClamped()
    {
        var (participant, _) = CreateParticipantWithBudget(3);
        participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);

        var outcome = participant.RetryPendingClaim(Passage);

        outcome.ShouldBe(ProgressOutcome.Clamped);
        participant.LastValidIndex.ShouldBe(2);
    }

    [Fact]
    public void RetryPendingClaim_AfterBudgetRefills_ShouldApplyPendingKeystrokes()
    {
        var (participant, clock) = CreateParticipantWithBudget(3);
        participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);
        clock.Advance(TimeSpan.FromSeconds(1));

        var outcome = participant.RetryPendingClaim(Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(4);
    }

    [Fact]
    public void KeystrokesArrivingWhileClamped_ShouldApplyInArrivalOrder()
    {
        var (participant, clock) = CreateParticipantWithBudget(3);
        participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);
        participant.UpdateProgress(KeystrokeHelper.Sequence("u"), Passage);
        clock.Advance(TimeSpan.FromSeconds(1));

        var outcome = participant.RetryPendingClaim(Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(5);
        participant.WrongTail.ShouldBeEmpty();
    }

    [Fact]
    public void RetryPendingClaim_WithoutPendingKeystrokes_ShouldBeAccepted()
    {
        var (participant, _) = CreateParticipantWithBudget(3);

        var outcome = participant.RetryPendingClaim(Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(-1);
    }

    [Fact]
    public void RetryPendingClaim_AfterFinish_ShouldBeNoOp()
    {
        var (participant, clock) = CreateParticipantWithBudget(3);
        participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);
        participant.MarkFinished(1);
        clock.Advance(TimeSpan.FromSeconds(1));

        var outcome = participant.RetryPendingClaim(Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(2);
    }

    [Fact]
    public void BacklogWithinLimit_ShouldBeClamped()
    {
        var (participant, _) = CreateParticipantWithBudget(slack: 3, maxPendingKeystrokes: 2);

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);

        outcome.ShouldBe(ProgressOutcome.Clamped);
    }

    [Fact]
    public void BacklogBeyondLimit_ShouldBeRejected()
    {
        var (participant, _) = CreateParticipantWithBudget(slack: 3, maxPendingKeystrokes: 2);

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("the qu"), Passage);

        outcome.ShouldBe(ProgressOutcome.Rejected("keystroke backlog exceeds limit"));
    }

    [Fact]
    public void BacklogBeyondLimit_ShouldDropPendingKeystrokes()
    {
        var (participant, clock) = CreateParticipantWithBudget(slack: 3, maxPendingKeystrokes: 2);
        participant.UpdateProgress(KeystrokeHelper.Sequence("the qu"), Passage);
        clock.Advance(TimeSpan.FromSeconds(1));

        participant.RetryPendingClaim(Passage);

        participant.LastValidIndex.ShouldBe(2);
    }

    [Fact]
    public void BacklogAccumulatedAcrossBatches_ShouldBeRejected()
    {
        var (participant, _) = CreateParticipantWithBudget(slack: 3, maxPendingKeystrokes: 2);
        participant.UpdateProgress(KeystrokeHelper.Sequence("the q"), Passage);

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("u"), Passage);

        outcome.ShouldBe(ProgressOutcome.Rejected("keystroke backlog exceeds limit"));
    }

    [Fact]
    public void BatchLargerThanLimitButWithinBudget_ShouldBeAccepted()
    {
        var (participant, _) = CreateParticipantWithBudget(slack: 9, maxPendingKeystrokes: 2);

        var outcome = participant.UpdateProgress(KeystrokeHelper.Sequence("the quick"), Passage);

        outcome.ShouldBe(ProgressOutcome.Accepted);
        participant.LastValidIndex.ShouldBe(8);
    }

    [Fact]
    public void GetAccuracy_ShouldBeShareOfValidCharactersWithoutMistakes()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress([.. KeystrokeHelper.Sequence("thx"), KeystrokeHelper.Backspace(), .. KeystrokeHelper.Sequence("e ")], Passage);

        participant.GetAccuracy().ShouldBe(75f);
    }

    [Fact]
    public void GetAccuracy_MistakesExceedValidCharacters_ShouldClampToZero()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("txxxxx"), Passage);

        participant.GetAccuracy().ShouldBe(0f);
    }

    [Fact]
    public void MarkFinished_ResultWordsTypedShouldEqualWordsTyped()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(KeystrokeHelper.Sequence("the quick"), Passage);

        participant.MarkFinished(1);

        participant.WordsTyped.ShouldBe(2);
        participant.Result!.WordsTyped.ShouldBe(participant.WordsTyped);
    }

    [Fact]
    public void MarkFinished_ResultFinishedAtShouldMatchParticipant()
    {
        var (participant, clock) = CreateParticipant();
        participant.Start();
        participant.UpdateProgress(KeystrokeHelper.Sequence("the"), Passage);
        clock.Advance(TimeSpan.FromSeconds(20));

        participant.MarkFinished(1);

        participant.FinishedAt.ShouldBe(clock.GetUtcNow());
        participant.Result!.FinishedAt.ShouldBe(participant.FinishedAt!.Value);
    }

    [Fact]
    public void ClockAdvancesAfterFinish_WpmShouldStayFrozen()
    {
        var (participant, clock) = CreateParticipant();
        participant.Start();
        clock.Advance(TimeSpan.FromSeconds(60));
        participant.UpdateProgress(KeystrokeHelper.Sequence("the"), Passage);
        participant.MarkFinished(1);
        var wpmAtFinish = participant.GetWPM();

        clock.Advance(TimeSpan.FromSeconds(60));

        participant.GetWPM().ShouldBe(wpmAtFinish);
        participant.Result!.WPM.ShouldBe(participant.GetWPM());
    }
}
