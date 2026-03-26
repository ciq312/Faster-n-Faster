namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public class RaceParticipantResult
{
    public Guid Id { get; private set; }
    public Guid LobbyId { get; private set; }
    public Guid LobbyPlayerId { get; private set; }
    public float WPM { get; private set; }

    /// <summary>Percentage from 0 to 100.</summary>
    public float Accuracy { get; private set; }
    public int MistakeCount { get; private set; }
    public int SymbolsTyped { get; private set; }
    public int WordsTyped { get; private set; }
    public int? FinishPosition { get; private set; }
    public DateTime FinishedAt { get; private set; }


    public RaceParticipantResult(
        Guid id,
        Guid lobbyPlayerId,
        float wpm,
        float accuracy,
        int mistakeCount,
        int totalTyped,
        int wordsTyped,
        int? finishPosition
    )
    {
        if (accuracy is < 0 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(accuracy),
                "Accuracy must be between 0 and 100."
            );

        if (mistakeCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(mistakeCount),
                "Mistake count cannot be negative."
            );

        if (finishPosition < 1)
            throw new ArgumentOutOfRangeException(
                nameof(finishPosition),
                "Finish position must be at least 1."
            );
        Id = id;
        LobbyPlayerId = lobbyPlayerId;
        WPM = wpm;
        Accuracy = accuracy;
        SymbolsTyped = totalTyped;
        WordsTyped = wordsTyped;
        MistakeCount = mistakeCount;
        FinishPosition = finishPosition;
        FinishedAt = DateTime.UtcNow;
    }
}
