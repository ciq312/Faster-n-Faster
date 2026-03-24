using Ardalis.GuardClauses;

namespace FasterNFaster.Api.Core.Entities.Lobby;

public class LobbyPlayer(User user, Lobby lobby, int joinOrder)
{
    public User User { get; private set; } = user;
    public int JoinOrder { get; private set; } = joinOrder;
    public string ConnectionId { get; private set; } = null!;
    public bool IsConnected { get; private set; } = true;
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
    public Lobby Lobby { get; private set; } = lobby;
    public ICollection<RaceResult> RaceResults { get; private set; } = new List<RaceResult>();
    public bool IsHost => User.Id == Lobby.HostId;


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
