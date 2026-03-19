namespace FasterNFaster.Api.Core.Entities;

public class RaceResult
{
    public Guid Id { get; private set; }
    public Guid LobbyId { get; private set; }
    public Guid LobbyPlayerId { get; private set; }
    public double GrossWpm { get; private set; }
    public double NetWpm { get; private set; }

    /// <summary>Percentage from 0 to 100.</summary>
    public double Accuracy { get; private set; }
    public int MistakeCount { get; private set; }
    public int FinishPosition { get; private set; }
    public DateTime FinishedAt { get; private set; }

    public Lobby Lobby { get; private set; } = null!;
    public LobbyPlayer LobbyPlayer { get; private set; } = null!;

    public RaceResult(
        Guid lobbyId,
        Guid lobbyPlayerId,
        double grossWpm,
        double netWpm,
        double accuracy,
        int mistakeCount,
        int finishPosition
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

        Id = Guid.NewGuid();
        LobbyId = lobbyId;
        LobbyPlayerId = lobbyPlayerId;
        GrossWpm = grossWpm;
        NetWpm = netWpm;
        Accuracy = accuracy;
        MistakeCount = mistakeCount;
        FinishPosition = finishPosition;
        FinishedAt = DateTime.UtcNow;
    }
}
