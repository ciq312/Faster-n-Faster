using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.Core.Entities.Races;

public class RaceParticipant
{
    public const int MaxOverflow = 6;

    private readonly Func<DateTime> now;
    private readonly ProgressBudget budget;
    private (int Index, string Typed, int Mistakes)? pendingClaim;

    public RaceParticipant(Guid id, string color, string nick, IAntiCheatPolicy policy, Func<DateTime>? now = null)
    {
        this.now = now ?? (() => DateTime.UtcNow);
        budget = new ProgressBudget(policy.MaxCharsPerSecond, policy.BudgetSlack, this.now);
        Id = id;
        Color = color;
        Nick = nick;
    }

    public string Nick { get; private set; }
    public Guid Id { get; private set; }
    public string Color { get; private set; }
    public int Index
    {
        get;
        private set
        {
            if (value < -1) throw new InvalidDataException("Invalid index");
            field = value;
        }
    } = -1;
    public string Typed { get; private set; } = "";
    public int WordsTyped { get; private set; }
    public int Mistakes
    {
        get;
        private set
        {
            if (value < 0) throw new InvalidDataException("Invalid mistakes number");
            field = value;
        }
    } = 0;
    public bool IsFinished { get; private set; }
    public int? FinishPosition { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public DateTime StartedAt { get; private set; }

    public RaceParticipantResult? Result { get; private set; } = null!;

    public ProgressOutcome UpdateProgress(int newIndex, string newTyped, int newMistakes, string passage)
    {
        if (IsFinished) return ProgressOutcome.Accepted;
        if (DidUserRefresh(newIndex, newTyped)) return ProgressOutcome.Accepted;

        var violatedRule = FindViolatedRule(newIndex, newTyped, passage);
        if (violatedRule is not null) return ProgressOutcome.Rejected(violatedRule);

        return ApplyGrant(newIndex, newTyped, newMistakes, passage);
    }

    public void Start()
    {
        StartedAt = now();
    }

    public ProgressOutcome RetryPendingClaim(string passage)
    {
        if (IsFinished || pendingClaim is not { } claim) return ProgressOutcome.Accepted;

        return ApplyGrant(claim.Index, claim.Typed, claim.Mistakes, passage);
    }

    private ProgressOutcome ApplyGrant(int claimedIndex, string claimedTyped, int claimedMistakes, string passage)
    {
        var typedCharacters = Math.Max(0, claimedTyped.Length - Typed.Length);
        var grantedCharacters = budget.Take(typedCharacters);
        var isClamped = grantedCharacters < typedCharacters;

        var grantedTyped = isClamped ? claimedTyped[..(Typed.Length + grantedCharacters)] : claimedTyped;
        var grantedIndex = isClamped ? Math.Min(claimedIndex, grantedTyped.Length - 1) : claimedIndex;

        Typed = grantedTyped;
        Index = grantedIndex;
        WordsTyped = CountWords(passage.AsSpan(0, grantedIndex + 1));
        Mistakes = Math.Max(Mistakes, Math.Max(claimedMistakes, CountObservedMistakes(grantedIndex, grantedTyped, passage)));

        pendingClaim = isClamped ? (claimedIndex, claimedTyped, claimedMistakes) : null;
        return isClamped ? ProgressOutcome.Clamped : ProgressOutcome.Accepted;
    }

    private static int CountWords(ReadOnlySpan<char> text)
    {
        var count = 0;
        var inWord = false;
        foreach (var c in text)
        {
            if (c == ' ')
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                count++;
            }
        }

        return count;
    }

    private static int CountObservedMistakes(int index, string typed, string passage)
    {
        var count = 0;
        var end = Math.Min(typed.Length, passage.Length);
        for (var i = index + 1; i < end; i++)
            if (typed[i] != passage[i]) count++;

        return count;
    }

    private bool DidUserRefresh(int newIndex, string typed) => newIndex == -1 && typed == "";

    private static string? FindViolatedRule(int newIndex, string newTyped, string passage)
    {
        if (newIndex < -1) return "index below start";
        if (newIndex + 1 > newTyped.Length) return "typed shorter than reported index";
        if (newIndex + 1 > passage.Length) return "reported index exceeds passage length";
        if (!newTyped.AsSpan(0, newIndex + 1).SequenceEqual(passage.AsSpan(0, newIndex + 1))) return "typed prefix does not match passage";
        if (newTyped.Length - 1 - newIndex > MaxOverflow) return "overflow exceeds limit";
        if (TypesSpaceAfterWrongCharacter(newTyped, newIndex)) return "space typed after wrong character";
        return null;
    }

    private static bool TypesSpaceAfterWrongCharacter(string typed, int index)
    {
        for (var i = index + 2; i < typed.Length; i++)
            if (typed[i] == ' ') return true;

        return false;
    }

    public void MarkFinished(int position, int wordsTyped)
    {
        IsFinished = true;
        FinishPosition = position;
        FinishedAt = now();
        Result = new RaceParticipantResult(
            Guid.NewGuid(), Id, Nick, GetWPM(), GetAccuracy(), Mistakes, wordsTyped, FinishPosition);
    }

    public void MarkWithdrawn()
    {
        IsFinished = true;
        FinishedAt = now();
    }

    public float GetWPM()
    {
        var minutesElapsed = (float)((FinishedAt ?? now()) - StartedAt).TotalMinutes;
        if (minutesElapsed <= 0) return 0;
        return WordsTyped / minutesElapsed;
    }

    public float GetAccuracy()
    {
        if (Index < 0) throw new InvalidOperationException("Index can't be negative");
        return Math.Clamp
        ((1 - (float)Mistakes / (Index + 1)) * 100, 0, 100);
    }
}
