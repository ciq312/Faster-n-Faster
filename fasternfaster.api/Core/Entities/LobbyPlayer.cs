using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FasterNFaster.Api.Core.Entities;

[Table("lobby_players")]
public class LobbyPlayer
{
    [Column("id")]
    public Guid Id { get; private set; }

    [Column("lobby_id")]
    public Guid LobbyId { get; private set; }

    [Column("user_id")]
    public Guid? UserId { get; private set; }

    [Column("display_name")]
    [MaxLength(30)]
    public string DisplayName { get; private set; } = null!;

    [Column("join_order")]
    public int JoinOrder { get; private set; }

    [Column("connection_id")]
    public string ConnectionId { get; private set; } = null!;

    [Column("is_connected")]
    public bool IsConnected { get; private set; } = true;

    [Column("joined_at")]
    public DateTime JoinedAt { get; private set; }

    public Lobby Lobby { get; private set; } = null!;
    public ICollection<RaceResult> RaceResults { get; private set; } = new List<RaceResult>();

    private LobbyPlayer() { } // EF constructor

    public LobbyPlayer(Guid lobbyId, string displayName, int joinOrder, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.");

        if (displayName.Length > 30)
            throw new ArgumentException("Display name must be 30 characters or fewer.");

        Id = Guid.NewGuid();
        LobbyId = lobbyId;
        DisplayName = displayName;
        JoinOrder = joinOrder;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
    }

    public void Disconnect()
    {
        IsConnected = false;
    }

    public void Reconnect(string connectionId)
    {
        ConnectionId = connectionId;
        IsConnected = true;
    }
}
