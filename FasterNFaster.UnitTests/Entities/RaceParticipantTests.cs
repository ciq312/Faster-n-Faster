using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Entities;

public class RaceParticipantTests
{
    private const string Passage = "the quick brown fox jumps over the lazy dog pepe lolo gege roro gsgs asdge eergwegro oiwernoiewnviown weoriweoieoif woerignwoeig";

    private static (RaceParticipant Participant, FakeClock Clock) CreateParticipant(IAntiCheatPolicy? policy = null)
    {
        var clock = new FakeClock();
        var participant = new RaceParticipant(Guid.NewGuid(), "#fff", "alice", policy ?? Unlimited(), clock.Func);
        return (participant, clock);
    }

    private static FakeAntiCheatPolicy Unlimited() => new(maxCharsPerSecond: 1000, budgetSlack: 1000);

    private static ConfiguredAntiCheatPolicy DefaultPolicy() =>
        new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));

    [Fact]
    public void UpdateProgress_AcceptsValidProgress_UpdatesAllFields()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(2, "the", 0, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(2, participant.Index);
        Assert.Equal("the", participant.Typed);
        Assert.Equal(0, participant.Mistakes);
        Assert.Equal(1, participant.WordsTyped);
    }

    [Fact]
    public void UpdateProgress_RefreshSignal_IsNoOp()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(2, "the", 0, Passage);

        var outcome = participant.UpdateProgress(-1, "", 0, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(2, participant.Index);
        Assert.Equal("the", participant.Typed);
    }

    [Fact]
    public void UpdateProgress_AlreadyFinished_IsNoOp()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(2, "the", 0, Passage);
        participant.MarkFinished(1, 1);

        participant.UpdateProgress(5, "the q", 0, Passage);

        Assert.Equal(2, participant.Index);
    }

    [Fact]
    public void UpdateProgress_OverflowOfSixCharacters_IsAccepted()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(2, "the", 0, Passage);

        var outcome = participant.UpdateProgress(2, "the" + "xxxxxx", 0, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(6, participant.Mistakes);
    }

    [Fact]
    public void UpdateProgress_SpaceAfterCorrectCharacter_IsAccepted()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(3, "the ", 0, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(3, participant.Index);
    }

    [Fact]
    public void UpdateProgress_TypedShorterThanIndex_IsRejected()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(5, "the", 0, Passage);

        Assert.Equal(ProgressOutcome.Rejected("typed shorter than reported index"), outcome);
        Assert.Equal(-1, participant.Index);
    }

    [Fact]
    public void UpdateProgress_IndexExceedsPassageLength_IsRejected()
    {
        var (participant, _) = CreateParticipant();
        var oversizedTyped = new string('x', Passage.Length + 5);

        var outcome = participant.UpdateProgress(Passage.Length + 2, oversizedTyped, 0, Passage);

        Assert.Equal(ProgressOutcome.Rejected("reported index exceeds passage length"), outcome);
        Assert.Equal(-1, participant.Index);
    }

    [Fact]
    public void UpdateProgress_TypedPrefixDoesNotMatch_IsRejected()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(2, "txe", 0, Passage);

        Assert.Equal(ProgressOutcome.Rejected("typed prefix does not match passage"), outcome);
        Assert.Equal("", participant.Typed);
    }

    [Fact]
    public void UpdateProgress_IndexBelowStart_IsRejected()
    {
        var (participant, _) = CreateParticipant();

        var outcome = participant.UpdateProgress(-5, "t", 0, Passage);

        Assert.Equal(ProgressOutcome.Rejected("index below start"), outcome);
        Assert.Equal(-1, participant.Index);
    }

    [Fact]
    public void UpdateProgress_OverflowOfSevenCharacters_IsRejected()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(2, "the", 0, Passage);

        var outcome = participant.UpdateProgress(2, "the" + "xxxxxxx", 0, Passage);

        Assert.Equal(ProgressOutcome.Rejected("overflow exceeds limit"), outcome);
        Assert.Equal("the", participant.Typed);
    }

    [Fact]
    public void UpdateProgress_SpaceAfterWrongCharacter_IsRejected()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(2, "the", 0, Passage);

        var outcome = participant.UpdateProgress(2, "thex ", 0, Passage);

        Assert.Equal(ProgressOutcome.Rejected("space typed after wrong character"), outcome);
        Assert.Equal("the", participant.Typed);
    }

    [Fact]
    public void UpdateProgress_TypingBeyondBudget_IsClampedToBudget()
    {
        var (participant, _) = CreateParticipant(DefaultPolicy());

        var outcome = participant.UpdateProgress(8, "the quick", 0, Passage);

        Assert.Equal(ProgressOutcome.Clamped, outcome);
        Assert.Equal("the qu", participant.Typed);
        Assert.Equal(5, participant.Index);
        Assert.Equal(2, participant.WordsTyped);
    }

    [Fact]
    public void UpdateProgress_AfterClamp_AcceptsOnceBudgetRefills()
    {
        var (participant, clock) = CreateParticipant(DefaultPolicy());
        participant.UpdateProgress(8, "the quick", 0, Passage);

        clock.Advance(TimeSpan.FromSeconds(1));
        var outcome = participant.UpdateProgress(8, "the quick", 0, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(8, participant.Index);
        Assert.Equal("the quick", participant.Typed);
    }

    [Fact]
    public void RetryPendingClaim_NoPendingClaim_IsAcceptedNoOp()
    {
        var (participant, _) = CreateParticipant(DefaultPolicy());

        var outcome = participant.RetryPendingClaim(Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(-1, participant.Index);
    }

    [Fact]
    public void RetryPendingClaim_BeforeBudgetRefills_StaysClampedAndKeepsPendingClaim()
    {
        var (participant, _) = CreateParticipant(DefaultPolicy());
        participant.UpdateProgress(8, "the quick", 0, Passage);

        var outcome = participant.RetryPendingClaim(Passage);

        Assert.Equal(ProgressOutcome.Clamped, outcome);
        Assert.Equal("the qu", participant.Typed);
        Assert.Equal(5, participant.Index);
    }

    [Fact]
    public void RetryPendingClaim_AfterBudgetRefills_GrantsTheFullOriginalClaim()
    {
        var (participant, clock) = CreateParticipant(DefaultPolicy());
        participant.UpdateProgress(8, "the quick", 0, Passage);

        clock.Advance(TimeSpan.FromSeconds(1));
        var outcome = participant.RetryPendingClaim(Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(8, participant.Index);
        Assert.Equal("the quick", participant.Typed);
    }

    [Fact]
    public void RetryPendingClaim_ParticipantAlreadyFinished_IsNoOp()
    {
        var (participant, _) = CreateParticipant(DefaultPolicy());
        participant.UpdateProgress(8, "the quick", 0, Passage);
        participant.MarkWithdrawn();

        var outcome = participant.RetryPendingClaim(Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal("the qu", participant.Typed);
    }

    [Fact]
    public void UpdateProgress_FreshUpdate_SupersedesStalePendingClaim()
    {
        var (participant, clock) = CreateParticipant(DefaultPolicy());
        participant.UpdateProgress(8, "the quick", 0, Passage);
        participant.UpdateProgress(11, "the quick br", 0, Passage);

        clock.Advance(TimeSpan.FromSeconds(1));
        var outcome = participant.RetryPendingClaim(Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(11, participant.Index);
        Assert.Equal("the quick br", participant.Typed);
    }

    [Fact]
    public void UpdateProgress_ReportedMistakesLowerThanStored_KeepsStoredCount()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(2, "the", 3, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(100));

        var outcome = participant.UpdateProgress(3, "the ", 1, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(3, participant.Mistakes);
    }

    [Fact]
    public void UpdateProgress_ReloadWithZeroMistakes_KeepsStoredCountAndIsAccepted()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(2, "the", 5, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(100));

        var outcome = participant.UpdateProgress(2, "the", 0, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(5, participant.Mistakes);
    }

    [Fact]
    public void UpdateProgress_WrongCharactersPastIndex_AreCountedAsMistakes()
    {
        var (participant, _) = CreateParticipant();

        participant.UpdateProgress(2, "thexyz", 0, Passage);

        Assert.Equal(3, participant.Mistakes);
    }

    [Fact]
    public void UpdateProgress_WrongCharactersPastPassageEnd_AreNotCountedAsMistakes()
    {
        var (participant, _) = CreateParticipant();

        participant.UpdateProgress(0, "azzzzzz", 0, "ab");

        Assert.Equal(1, participant.Mistakes);
    }

    [Fact]
    public void UpdateProgress_NegativeReportedMistakes_KeepsStoredCount()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(2, "the", 2, Passage);

        var outcome = participant.UpdateProgress(2, "the", -1, Passage);

        Assert.Equal(ProgressOutcome.Accepted, outcome);
        Assert.Equal(2, participant.Mistakes);
    }

    [Fact]
    public void GetAccuracy_ReturnsPercentageOfCorrectCharacters()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(3, "the ", 1, Passage);

        Assert.Equal(75f, participant.GetAccuracy());
    }

    [Fact]
    public void GetAccuracy_MistakesExceedTypedCharacters_ClampsToZero()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(0, "t", 5, Passage);

        Assert.Equal(0f, participant.GetAccuracy());
    }
}
