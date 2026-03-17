using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FasterNFaster.Api.Core.Entities;

[Table("lobbies")]
public class Lobby
{
    private static readonly HashSet<string> ValidGameModes = new() { "word_count", "timer" };

    // waiting -> racing -> finished
    private static readonly Dictionary<string, string> AllowedTransitions = new()
    {
        { "waiting", "racing" },
        { "racing", "finished" },
    };

    [Column("id")]
    public Guid Id { get; private set; }

    [Column("game_mode")]
    public string GameMode { get; private set; } = null!;

    [Column("is_private")]
    public bool IsPrivate { get; private set; }

    [Column("invite_code")]
    [MaxLength(8)]
    public string? InviteCode { get; private set; }

    [Column("status")]
    public string Status { get; private set; } = "waiting";

    [Column("host_player_id")]
    public Guid? HostPlayerId { get; private set; }

    [Column("max_players")]
    public int MaxPlayers { get; private set; } = 30;

    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; private set; }

    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();
    public ICollection<RaceResult> RaceResults { get; private set; } = new List<RaceResult>();

    private Lobby() { } // EF constructor

    public Lobby(string gameMode, bool isPrivate, string? inviteCode = null)
    {
        if (!ValidGameModes.Contains(gameMode))
            throw new ArgumentException(
                $"Invalid game mode: {gameMode}. Must be one of: {string.Join(", ", ValidGameModes)}"
            );

        if (isPrivate && string.IsNullOrWhiteSpace(inviteCode))
            throw new ArgumentException("Private lobbies require an invite code.");

        if (!isPrivate && inviteCode is not null)
            throw new ArgumentException("Public lobbies must not have an invite code.");

        if (inviteCode is not null && inviteCode.Length > 8)
            throw new ArgumentException("Invite code must be 8 characters or fewer.");

        Id = Guid.NewGuid();
        GameMode = gameMode;
        IsPrivate = isPrivate;
        InviteCode = inviteCode;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransitionStatus(string newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var expected) || expected != newStatus)
            throw new InvalidOperationException(
                $"Cannot transition from '{Status}' to '{newStatus}'."
            );

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignHost(Guid? playerId)
    {
        HostPlayerId = playerId;
        UpdatedAt = DateTime.UtcNow;
    }
}
