using Ardalis.GuardClauses;

namespace FasterNFaster.Api.Core.Entities;

public class LobbyPlayer
{
    public Guid PlayerId { get; private set; }
    public Guid LobbyPlayerId { get; private set; }
    public Guid LobbyId { get; private set; }
    public int JoinOrder { get; private set; }
    public string ConnectionId { get; private set; } = null!;
    public bool IsConnected { get; private set; } = true;
    public DateTime JoinedAt { get; private set; }

    public Lobby Lobby { get; private set; } = null!;
    public ICollection<RaceResult> RaceResults { get; private set; } = new List<RaceResult>();

    public LobbyPlayer(Guid userId, Guid lobbyId)
    {
        LobbyPlayerId = new Guid();
        PlayerId = userId;
        LobbyId = lobbyId;
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
