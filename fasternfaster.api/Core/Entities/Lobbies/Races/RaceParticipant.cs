using FasterNFaster.Api.Core.Exceptions.Lobbies.Races;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public class RaceParticipant
{
    const double MAX_WPM_POSSIBLE = 1500;
    const double MAX_CHARS_PER_SECOND = MAX_WPM_POSSIBLE * 5 / 60; // ~25 chars per second

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
    /// <returns>true if the update was accepted (possibly clamped)</returns>
    /// can ban | can already be finished 
    public void UpdateProgress(int newIndex, string newTyped, int newMistakes, string passage)
    {
        if (IsFinished)
            return;

        if (DidUserRefresh(newIndex, newTyped)) return;

        ValidateUpdate(newIndex, newTyped, passage, newMistakes);

        Typed = newTyped;
        Index = newIndex;
        WordsTyped = passage[..(newIndex + 1)].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        Mistakes = newMistakes;
        LastUpdateAt = _now();
    }

    private bool DidUserRefresh(int newIndex, string typed) => newIndex == -1 && typed == "";

    private void ValidateUpdate(int newIndex, string newTyped, string passage, int newMistakes)
    {
        // ValidateWPM(newIndex);
        ValidateIndexCorrespondence(newIndex, newTyped, passage);
        ValidateMistakes(newMistakes);
    }

    private void ValidateWPM(int newIndex)
    {
        var now = _now();
        double secondsElapsed = (now - LastUpdateAt).TotalSeconds;
        int indexDelta = newIndex - Index;

        if (secondsElapsed > 0 && indexDelta > 0)
        {
            double charsPerSecond = indexDelta / secondsElapsed;
            if (charsPerSecond > MAX_CHARS_PER_SECOND) throw new CheaterDetectedException("typing speed exceeds human limit");
        }
    }
    private void ValidateIndexCorrespondence(int newIndex, string newTyped, string passage)
    {
        if (newIndex + 1 > newTyped.Length) throw new CheaterDetectedException("typed shorter than reported index");
        if (newIndex + 1 > passage.Length) throw new CheaterDetectedException("reported index exceeds passage length");
        if (newTyped[..(newIndex + 1)] != passage[..(newIndex + 1)]) throw new CheaterDetectedException("typed prefix does not match passage");
    }
    private void ValidateMistakes(int newMistakes)
    {
        if (newMistakes < Mistakes) throw new CheaterDetectedException("mistakes count decreased");
    }

    public void MarkFinished(int position, int wordsTyped)
    {
#if DEBUG
        Log.Information($"finish position {position} for player {Nick} with wpm {GetWPM():F2} and accuracy {GetAccuracy():P2}");
#endif
        IsFinished = true;
        FinishPosition = position;
        FinishedAt = _now();
        Result = new RaceParticipantResult(
               Guid.NewGuid()
               , Id
               , Nick
               , GetWPM()
               , GetAccuracy()
               , Mistakes
               , wordsTyped
               , FinishPosition);
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
        return 1 - (float)Mistakes / (Index + 1);
    }

}
