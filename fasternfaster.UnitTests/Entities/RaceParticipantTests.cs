using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Exceptions.Races;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Entities;

public class RaceParticipantTests
{
    private const string Passage = "the quick brown fox jumps over the lazy dog pepe lolo gege roro gsgs asdge eergwegro oiwernoiewnviown weoriweoieoif woerignwoeig";

    private static (RaceParticipant Participant, FakeClock Clock) CreateParticipant()
    {
        var clock = new FakeClock();
        var participant = new RaceParticipant(Guid.NewGuid(), "#fff", "alice", clock.Func);
        return (participant, clock);
    }

    private static ConfiguredAntiCheatPolicy DefaultPolicy() =>
        new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));

    // -------- happy path --------
    [Fact]
    public void ValidateWpm_ShouldNotThrowOnEarlyBurst()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(0, "t", 0, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(50));

        // delta < BurstMinIndexDelta (15), so burst check is skipped
        AntiCheatCheck.ValidateWPM(participant, 10, DefaultPolicy(), clock.Now);
    }

    [Fact]
    public void ValidateWpm_ShouldNotThrowOnBurstLowerMaxPossible()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(0, "t", 0, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(3000));
        participant.UpdateProgress(20, "the quick brown fox j", 0, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(50));

        // 5 chars in 50ms ≈ 100 chars/sec, within default burst limit
        AntiCheatCheck.ValidateWPM(participant, 25, DefaultPolicy(), clock.Now);
    }

    [Fact]
    public void ValidateWpm_ShouldNotThrowOnDefaultSustainedWpm()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(0, "t", 0, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(3000));

        // 30 index ~ 6 words ~ 240 wpm — within default sustained limit
        AntiCheatCheck.ValidateWPM(participant, 30, DefaultPolicy(), clock.Now);
    }

    [Fact]
    public void UpdateProgress_AcceptsValidProgress_UpdatesAllFields()
    {
        var (participant, clock) = CreateParticipant();

        participant.UpdateProgress(2, "the", 0, Passage);

        Assert.Equal(2, participant.Index);
        Assert.Equal("the", participant.Typed);
        Assert.Equal(0, participant.Mistakes);
        Assert.Equal(1, participant.WordsTyped);
        Assert.Equal(clock.Now, participant.LastUpdateAt);
    }

    [Fact]
    public void UpdateProgress_RefreshSignal_IsNoOp()
    {
        var (participant, _) = CreateParticipant();
        participant.UpdateProgress(2, "the", 0, Passage);

        participant.UpdateProgress(-1, "", 0, Passage);

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

    // -------- WPM --------
    [Fact]
    public void ValidateWpm_ThrowsOnTooHighBurstAfterMinIndex()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(0, "t", 0, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(100));

        var ex = Assert.Throws<CheaterDetectedException>(() =>
            AntiCheatCheck.ValidateWPM(participant, 15, DefaultPolicy(), clock.Now));

        Assert.Equal("Burst wpm", ex.Reason);
    }

    [Fact]
    public void ValidateWpm_ThrowsOnTooHighSustainedAfterMinIndex()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(0, "t", 0, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(1000));

        // ~6 words in 1 sec ≈ 360 wpm, over default 300 wpm limit
        var ex = Assert.Throws<CheaterDetectedException>(() =>
            AntiCheatCheck.ValidateWPM(participant, 30, DefaultPolicy(), clock.Now));

        Assert.Equal("Sustained wpm", ex.Reason);
    }

    [Fact]
    public void ValidateWpm_DoesNotThrow_WhenIndexUnchanged()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(2, "the", 0, Passage);
        clock.Advance(TimeSpan.FromMilliseconds(100));

        // same index → delta = 0, burst check skipped
        AntiCheatCheck.ValidateWPM(participant, 2, DefaultPolicy(), clock.Now);
        participant.UpdateProgress(2, "the", 1, Passage);

        Assert.Equal(2, participant.Index);
        Assert.Equal(1, participant.Mistakes);
    }

    [Fact]
    public void ValidateWpm_DoesNotThrow_OnFirstCallWhenZeroSecondsElapsed()
    {
        var (participant, clock) = CreateParticipant();

        // no time has passed → secondsSinceLastUpdate == 0, burst check skipped
        AntiCheatCheck.ValidateWPM(participant, 2, DefaultPolicy(), clock.Now);
        participant.UpdateProgress(2, "the", 0, Passage);

        Assert.Equal(2, participant.Index);
    }

    // -------- Index correspondence --------

    [Fact]
    public void ValidateIndex_ThrowsCheater_WhenTypedShorterThanIndex()
    {
        var (participant, _) = CreateParticipant();

        var ex = Assert.Throws<CheaterDetectedException>(
            () => participant.UpdateProgress(5, "the", 0, Passage));
        Assert.Equal("typed shorter than reported index", ex.Reason);
    }

    [Fact]
    public void ValidateIndex_ThrowsCheater_WhenIndexExceedsPassageLength()
    {
        var (participant, _) = CreateParticipant();
        var oversizedTyped = new string('x', Passage.Length + 5);

        var ex = Assert.Throws<CheaterDetectedException>(
            () => participant.UpdateProgress(Passage.Length + 2, oversizedTyped, 0, Passage));
        Assert.Equal("reported index exceeds passage length", ex.Reason);
    }

    [Fact]
    public void ValidateIndex_ThrowsCheater_WhenTypedPrefixDoesNotMatchPassage()
    {
        var (participant, _) = CreateParticipant();

        var ex = Assert.Throws<CheaterDetectedException>(
            () => participant.UpdateProgress(2, "txe", 0, Passage));
        Assert.Equal("typed prefix does not match passage", ex.Reason);
    }

    [Fact]
    public void ValidateIndex_Accepts_WhenTypedHasOverflowAfterIndex()
    {
        var (participant, _) = CreateParticipant();

        // index points at 'e' (passage[2]); "wzzz" overflow past unclaimed prefix
        participant.UpdateProgress(2, "thewzzz", 0, Passage);

        Assert.Equal(2, participant.Index);
        Assert.Equal("thewzzz", participant.Typed);
    }

    // -------- Mistakes --------

    [Fact]
    public void ValidateMistakes_ThrowsCheater_WhenNewMistakesLowerThanCurrent()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(2, "the", 3, Passage);

        clock.Advance(TimeSpan.FromMilliseconds(100));

        var ex = Assert.Throws<CheaterDetectedException>(
            () => participant.UpdateProgress(3, "the ", 1, Passage));
        Assert.Equal("mistakes count decreased", ex.Reason);
    }

    [Fact]
    public void ValidateMistakes_Accepts_WhenEqual()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(2, "the", 3, Passage);

        clock.Advance(TimeSpan.FromMilliseconds(100));
        participant.UpdateProgress(3, "the ", 3, Passage);

        Assert.Equal(3, participant.Mistakes);
    }

    [Fact]
    public void ValidateMistakes_Accepts_WhenIncreased()
    {
        var (participant, clock) = CreateParticipant();
        participant.UpdateProgress(2, "the", 1, Passage);

        clock.Advance(TimeSpan.FromMilliseconds(100));
        participant.UpdateProgress(3, "the ", 2, Passage);

        Assert.Equal(2, participant.Mistakes);
    }
}
