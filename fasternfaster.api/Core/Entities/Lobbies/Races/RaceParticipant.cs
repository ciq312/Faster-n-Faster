namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public class RaceParticipant(Guid id, string color, string nick)
{
    const int MAX_INDEX_JUMP = 5;
    const double MAX_CHARS_PER_SECOND = 20;
    const int AVERAGE_WORD_LENGTH = 5;

    public string Nick { get; private set; } = nick;
    public Guid Id { get; private set; } = id;
    public string Color { get; private set; } = color;
    public int Index { get; private set; }
    public int TotalTyped { get; private set; }
    public int WordsTyped { get; private set; }
    public int Mistakes { get; private set; }
    public bool IsFinished { get; private set; }
    public int? FinishPosition { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastUpdateAt { get; private set; } = DateTime.UtcNow;

    public RaceParticipantResult? Result { get; private set; } = null!;


    /// <summary>
    /// Validates and applies a client state snapshot. Clamps invalid values instead of rejecting.
    /// </summary>
    /// <returns>true if the update was accepted (possibly clamped)</returns>
    public bool ValidateUpdate(int newIndex, int newTotalTyped, int newMistakes, int passageLength)
    {
        if (IsFinished)
            return false;

        if (newIndex < 0 || newTotalTyped < 0 || newMistakes < 0)
            return false;

        if (newTotalTyped < newIndex)
            return false;

        if (newMistakes > newTotalTyped)
            return false;

        int indexDelta = newIndex - Index;
        if (indexDelta > MAX_INDEX_JUMP)
            newIndex = Index + MAX_INDEX_JUMP;

        var now = DateTime.UtcNow;
        double secondsElapsed = (now - LastUpdateAt).TotalSeconds;
        if (secondsElapsed > 0 && indexDelta > 0)
        {
            double charsPerSecond = indexDelta / secondsElapsed;
            if (charsPerSecond > MAX_CHARS_PER_SECOND)
                newIndex = Index + (int)(MAX_CHARS_PER_SECOND * secondsElapsed);
        }

        if (newIndex > passageLength)
            newIndex = passageLength;

        Index = newIndex;
        TotalTyped = newTotalTyped;
        WordsTyped = (Index - Mistakes + 1) / AVERAGE_WORD_LENGTH;
        Mistakes = newMistakes;
        LastUpdateAt = now;


        return true;
    }

    public void MarkFinished(int position)
    {
        IsFinished = true;
        FinishPosition = position;
        FinishedAt = DateTime.UtcNow;
        Result = new RaceParticipantResult(
               Guid.NewGuid()
               , Id
               , Nick
               , GetWPM()
               , GetAccuracy()
               , Mistakes
               , TotalTyped
               , WordsTyped
               , FinishPosition);
    }

    public float GetWPM()
    {
        float minutesElapsed = (float)(DateTime.UtcNow - StartedAt).TotalMinutes;
        if (minutesElapsed <= 0) return 0;
        return WordsTyped / minutesElapsed;
    }

    public float GetAccuracy()
    {
        return 1 - (float)Mistakes / TotalTyped;
    }

}
