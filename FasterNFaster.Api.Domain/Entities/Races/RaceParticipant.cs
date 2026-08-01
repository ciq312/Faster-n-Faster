using FasterNFaster.Api.Core.Exceptions.Races;

namespace FasterNFaster.Api.Core.Entities.Races;

public class RaceParticipant
{
    private readonly Func<DateTime> _now;

    public RaceParticipant(Guid id, string color, string nick, Func<DateTime>? now = null)
    {
        _now = now ?? (() => DateTime.UtcNow);
        Id = id;
        Color = color;
        Nick = nick;
        StartedAt = _now();
        LastUpdateAt = _now();
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
    public DateTime LastUpdateAt { get; private set; }

    public RaceParticipantResult? Result { get; private set; } = null!;

    /// <summary>
    /// Validates and applies a client state snapshot. Clamps invalid values instead of rejecting.
    /// </summary>
    public void UpdateProgress(int newIndex, string newTyped, int newMistakes, string passage)
    {
        if (IsFinished) return;
        if (DidUserRefresh(newIndex, newTyped)) return;

        ValidateIndexCorrespondence(newIndex, newTyped, passage);
        ValidateMistakes(newMistakes);

        Typed = newTyped;
        Index = newIndex;
        WordsTyped = CountWords(passage.AsSpan(0, newIndex + 1));
        Mistakes = newMistakes;
        LastUpdateAt = _now();
    }

    private static int CountWords(ReadOnlySpan<char> text)
    {
        var count = 0;
        var inWord = false;
        foreach (var c in text)
        {
            if (c == ' ')
                inWord = false;
            else if (!inWord)
            {
                inWord = true;
                count++;
            }
        }

        return count;
    }

    private bool DidUserRefresh(int newIndex, string typed) => newIndex == -1 && typed == "";

    private void ValidateIndexCorrespondence(int newIndex, string newTyped, string passage)
    {
        if (newIndex + 1 > newTyped.Length) throw new CheaterDetectedException("typed shorter than reported index");
        if (newIndex + 1 > passage.Length) throw new CheaterDetectedException("reported index exceeds passage length");
        if (!newTyped.AsSpan(0, newIndex + 1).SequenceEqual(passage.AsSpan(0, newIndex + 1))) throw new CheaterDetectedException("typed prefix does not match passage");
    }

    private void ValidateMistakes(int newMistakes)
    {
        if (newMistakes < Mistakes) throw new CheaterDetectedException("mistakes count decreased");
    }

    public void MarkFinished(int position, int wordsTyped)
    {
        IsFinished = true;
        FinishPosition = position;
        FinishedAt = _now();
        Result = new RaceParticipantResult(
            Guid.NewGuid(), Id, Nick, GetWPM(), GetAccuracy(), Mistakes, wordsTyped, FinishPosition);
    }

    public void MarkWithdrawn()
    {
        IsFinished = true;
        FinishedAt = _now();
    }

    public float GetWPM()
    {
        float minutesElapsed = (float)(_now() - StartedAt).TotalMinutes;
        if (minutesElapsed <= 0) return 0;
        return WordsTyped / minutesElapsed;
    }

    public float GetAccuracy()
    {
        if (Index < 0) throw new InvalidOperationException("Index can't be negative");
        return (1 - (float)Mistakes / (Index + 1)) * 100;
    }
}
