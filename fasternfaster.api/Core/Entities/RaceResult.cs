using System.ComponentModel.DataAnnotations.Schema;

namespace FasterNFaster.Api.Core.Entities;

[Table("race_results")]
public class RaceResult
{
    [Column("id")]
    public Guid Id { get; private set; }

    [Column("lobby_id")]
    public Guid LobbyId { get; private set; }

    [Column("lobby_player_id")]
    public Guid LobbyPlayerId { get; private set; }

    [Column("gross_wpm")]
    public double GrossWpm { get; private set; }

    [Column("net_wpm")]
    public double NetWpm { get; private set; }

    /// <summary>Percentage from 0 to 100.</summary>
    [Column("accuracy")]
    public double Accuracy { get; private set; }

    [Column("mistake_count")]
    public int MistakeCount { get; private set; }

    [Column("finish_position")]
    public int FinishPosition { get; private set; }

    [Column("finished_at")]
    public DateTime FinishedAt { get; private set; }

    public Lobby Lobby { get; private set; } = null!;
    public LobbyPlayer LobbyPlayer { get; private set; } = null!;

    private RaceResult() { } // EF constructor

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
