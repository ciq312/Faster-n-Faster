namespace FasterNFaster.Api.Core.RaceState;

public class RaceParticipant
{
    public Guid PlayerId { get; }
    public string DisplayName { get; }
    public string ConnectionId { get; private set; }

    public int CurrentIndex { get; private set; }
    public int Mistakes { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    public bool HasFinished => FinishedAt.HasValue;

    public RaceParticipant(Guid playerId, string displayName, string connectionId)
    {
        PlayerId = playerId;
        DisplayName = displayName;
        ConnectionId = connectionId;
    }

    public void UpdateConnection(string connectionId) => ConnectionId = connectionId;

    /// <summary>
    /// Validates the typed character against the passage at the current index.
    /// Advances the index on correct input, records a mistake on incorrect.
    /// </summary>
    /// <returns>True if the character was correct.</returns>
    public bool RecordKeystroke(char typed, string passage)
    {
        if (typed == passage[CurrentIndex])
        {
            CurrentIndex++;
            return true;
        }

        Mistakes++;
        return false;
    }

    /// <summary>
    /// Checks if the participant has typed the full passage. Marks as finished if so.
    /// </summary>
    /// <returns>True if the participant just finished.</returns>
    public bool CheckFinished(string passage)
    {
        if (HasFinished || CurrentIndex < passage.Length)
            return false;

        FinishedAt = DateTime.UtcNow;
        return true;
    }
}
