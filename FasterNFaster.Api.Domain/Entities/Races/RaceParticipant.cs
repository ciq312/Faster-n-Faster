using FasterNFaster.Api.Domain.Entities.Keys;
using FasterNFaster.Api.Domain.Interfaces;

namespace FasterNFaster.Api.Domain.Entities.Races;

public class RaceParticipant
{
    public const int MaxOverflow = 6;
    private readonly ProgressBudget budget;
    private readonly TimeProvider timeProvider;
    private readonly Queue<Keystroke> pendingKeystrokes = new();

    public RaceParticipant(Guid id, string color, string nick, IAntiCheatPolicy policy, TimeProvider timeProvider)
    {
        budget = new ProgressBudget(policy.MaxCharsPerSecond, policy.BudgetSlack, timeProvider);
        Id = id;
        Color = color;
        Nick = nick;
        this.timeProvider = timeProvider;
    }

    public string Nick { get; private set; }
    public Guid Id { get; private set; }
    public string Color { get; private set; }
    public int LastValidIndex
    {
        get;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
            field = value;
        }
    } = -1;
    public string WrongTail { get; private set; } = "";
    private int Index => LastValidIndex + WrongTail.Length;
    public int WordsTyped { get; private set; }
    public int Mistakes { get; private set; } = 0;
    public bool IsFinished { get; private set; }
    public int? FinishPosition { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public RaceParticipantResult? Result { get; private set; }

    public ProgressOutcome UpdateProgress(Keystroke[] keystrokes, string passage)
    {
        if (IsFinished) return ProgressOutcome.Accepted;

        foreach (var key in keystrokes)
        {
            pendingKeystrokes.Enqueue(key);
        }

        return ApplyPending(passage);
    }

    private ProgressOutcome ApplyPending(string passage)
    {
        var granted = budget.Take(pendingKeystrokes.Count);

        for (var i = 0; i < granted; i++)
        {
            var violatedRule = ApplyKeystroke(pendingKeystrokes.Dequeue(), passage);
            if (violatedRule is null) continue;
            return ProgressOutcome.Rejected(violatedRule);
        }

        return pendingKeystrokes.Count > 0 ? ProgressOutcome.Clamped : ProgressOutcome.Accepted;
    }

    private string? ApplyKeystroke(Keystroke keystroke, string passage)
    {
        if (LastValidIndex >= passage.Length - 1) return null;

        switch (keystroke)
        {
            case Backspace:
                if (WrongTail.Length > 0)
                {
                    WrongTail = WrongTail[..^1];
                }
                else if (LastValidIndex > -1)
                {
                    LastValidIndex--;
                    WordsTyped = CountWords(passage.AsSpan(0, LastValidIndex + 1));
                }
                return "Backspace on empty text";
            case CharTyped charTyped:
                var newIndex = Index + 1;
                var isExpected = newIndex < passage.Length && passage[newIndex] == charTyped.Value;

                if (WrongTail.Length > 0)
                {
                    if (charTyped.Value == ' ')
                    {
                        return "space typed after wrong character";
                    }
                    if (WrongTail.Length >= MaxOverflow)
                    {
                        return "overflow exceeds limit";
                    }

                    WrongTail += charTyped.Value;
                    if (!isExpected) Mistakes++;
                    return null;
                }

                if (isExpected)
                {
                    LastValidIndex = newIndex;
                    WordsTyped = CountWords(passage.AsSpan(0, LastValidIndex + 1));
                }
                else
                {
                    WrongTail += charTyped.Value;
                    Mistakes++;
                }
                return null;
        }
        return null;
    }

    public void Start()
    {
        StartedAt = timeProvider.GetUtcNow();
    }

    public ProgressOutcome RetryPendingClaim(string passage)
    {
        if (IsFinished || pendingKeystrokes.Count == 0) return ProgressOutcome.Accepted;
        return ApplyPending(passage);
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

    public void MarkFinished(int position)
    {
        IsFinished = true;
        FinishPosition = position;
        var now = timeProvider.GetUtcNow();
        FinishedAt = now;
        Result = new RaceParticipantResult(
            Guid.NewGuid(), Id, Nick, GetWPM(), GetAccuracy(), Mistakes, WordsTyped, FinishPosition, now);
    }

    public void MarkWithdrawn()
    {
        IsFinished = true;
        FinishedAt = timeProvider.GetUtcNow();
    }

    public float GetWPM()
    {
        var minutesElapsed = (float)((FinishedAt ?? timeProvider.GetUtcNow()) - StartedAt).TotalMinutes;
        if (minutesElapsed <= 0) return 0;
        return WordsTyped / minutesElapsed;
    }

    public float GetAccuracy()
    {
        return Math.Clamp
        ((1 - (float)Mistakes / (LastValidIndex + 1)) * 100, 0, 100);
    }
}
