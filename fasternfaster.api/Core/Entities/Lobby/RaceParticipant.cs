namespace FasterNFaster.Api.Core.Entities.Lobby;

public class RaceParticipant
{
    // Anti-cheat limits
    const int MAX_INDEX_JUMP = 5;
    const double MAX_CHARS_PER_SECOND = 20; // ~240 WPM, well above human limit
    const int AVERAGE_WORD_LENGTH = 5;

    public Guid PlayerId { get; private set; }
    public int Index { get; private set; }
    public int TotalTyped { get; private set; }
    public int Mistakes { get; private set; }
    public bool IsFinished { get; private set; }
    public int? FinishPosition { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime LastUpdateAt { get; private set; }

    public RaceParticipant(Guid playerId)
    {
        PlayerId = playerId;
        StartedAt = DateTime.UtcNow;
        LastUpdateAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates and applies a client state snapshot. Clamps invalid values instead of rejecting.
    /// </summary>
    /// <returns>true if the update was accepted (possibly clamped)</returns>
    public bool ValidateUpdate(int newIndex, int newTotalTyped, int newMistakes, int passageLength)
    {
        if (IsFinished)
            return false;

        // Sanity checks — reject completely invalid data
        if (newIndex < 0 || newTotalTyped < 0 || newMistakes < 0)
            return false;

        if (newTotalTyped < newIndex)
            return false;

        if (newMistakes > newTotalTyped)
            return false;

        // Index can't go backward
        if (newIndex < Index)
            newIndex = Index;

        // Clamp index jump
        int indexDelta = newIndex - Index;
        if (indexDelta > MAX_INDEX_JUMP)
            newIndex = Index + MAX_INDEX_JUMP;

        // Speed check
        var now = DateTime.UtcNow;
        double secondsElapsed = (now - LastUpdateAt).TotalSeconds;
        if (secondsElapsed > 0 && indexDelta > 0)
        {
            double charsPerSecond = indexDelta / secondsElapsed;
            if (charsPerSecond > MAX_CHARS_PER_SECOND)
                newIndex = Index + (int)(MAX_CHARS_PER_SECOND * secondsElapsed);
        }

        // Clamp to passage length
        if (newIndex > passageLength)
            newIndex = passageLength;

        Index = newIndex;
        TotalTyped = newTotalTyped;
        Mistakes = newMistakes;
        LastUpdateAt = now;

        return true;
    }

    public void MarkFinished(int position)
    {
        IsFinished = true;
        FinishPosition = position;
        FinishedAt = DateTime.UtcNow;
    }

    public double GetWpm()
    {
        double minutesElapsed = (DateTime.UtcNow - StartedAt).TotalMinutes;
        if (minutesElapsed <= 0) return 0;
        return (Index / (double)AVERAGE_WORD_LENGTH) / minutesElapsed;
    }

    public double GetAccuracy()
    {
        if (TotalTyped == 0) return 0;
        return (double)Index / TotalTyped * 100;
    }
}
