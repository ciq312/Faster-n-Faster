namespace FasterNFaster.Api.Core.Entities.Lobby;

public class Lobby
{
    public enum Status
    {
        waiting,
        racing,
        finished,
    }

    private static readonly Dictionary<Status, Status> AllowedTransitions = new()
    {
        { Status.waiting, Status.racing },
        { Status.racing, Status.finished },
    };

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Status CurrentStatus { get; private set; }
    public Guid HostId { get; private set; }
    public LobbySettings LobbySettings { get; private set; }
    public Race? Race { get; private set; }

    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();
    public ICollection<RaceResult> RaceResults { get; private set; } = new List<RaceResult>();

    public Lobby(string name, bool isPrivate)
    {
        Id = Guid.NewGuid();
        Name = name;
        LobbySettings = new LobbySettings(isPrivate);
        CurrentStatus = Status.waiting;
    }

    public Race GetRace() =>
        Race ?? throw new InvalidOperationException("Race has not been configured.");

    public void ConfigureRace(Race race)
    {
        Race = race;
        LobbySettings.UpdateTimestamp();
    }

    public void ConfigureRace(Guid hostId, Race newRace)
    {
        ValidateHost(hostId);

        if (CurrentStatus != Status.waiting)
            throw new InvalidOperationException("Can only configure race while waiting.");

        Race = newRace;
        LobbySettings.UpdateTimestamp();
    }

    public void TransitionStatus(Status newStatus)
    {
        if (
            !AllowedTransitions.TryGetValue(CurrentStatus, out var expected)
            || expected != newStatus
        )
            throw new InvalidOperationException(
                $"Cannot transition from '{CurrentStatus}' to '{newStatus}'."
            );

        CurrentStatus = newStatus;
        LobbySettings.UpdateTimestamp();
    }

    private readonly object _lock = new();

    public LobbyPlayer AddPlayer(User user)
    {
        lock (_lock)
        {
            if (CurrentStatus != Status.waiting)
                throw new InvalidOperationException("Lobby is not accepting players.");

            if (Players.Count >= LobbySettings.MaxPlayers)
                throw new InvalidOperationException("Lobby is full.");

            var joinOrder = Players.Any() ? Players.Max(p => p.JoinOrder) + 1 : 1;
            var player = new LobbyPlayer(user, this, joinOrder);
            Players.Add(player);
            LobbySettings.UpdateTimestamp();
            return player;
        }
    }

    public void AssignHost(Guid hostId)
    {
        HostId = hostId;
        LobbySettings.UpdateTimestamp();
    }


    public void ValidateHost(Guid userId)
    {
        if (HostId != userId)
            throw new InvalidOperationException("Only the host can perform this action.");
    }

    public void TransferHost(Guid hostId, Guid newHostId)
    {
        lock (_lock)
        {
            ValidateHost(hostId);

            if (hostId == newHostId)
                throw new InvalidOperationException("Cannot transfer host to yourself.");

            var target =
                Players.FirstOrDefault(p => p.User.Id == newHostId && p.IsConnected)
                ?? throw new InvalidOperationException(
                    "Target player is not in this lobby or is disconnected."
                );

            AssignHost(newHostId);
        }
    }

    public void StartRace(Guid hostId)
    {
        lock (_lock)
        {
            ValidateHost(hostId);

            if (Race == null)
                throw new InvalidOperationException("Race has not been configured.");

            TransitionStatus(Status.racing);

            var connectedPlayerIds = Players
                .Where(p => p.IsConnected)
                .Select(p => p.User.Id);
            Race.Start(connectedPlayerIds);
        }
    }

    public LobbyPlayer KickPlayer(Guid hostId, Guid targetPlayerId)
    {
        lock (_lock)
        {
            ValidateHost(hostId);

            if (hostId == targetPlayerId)
                throw new InvalidOperationException("Cannot kick yourself.");

            var target =
                Players.FirstOrDefault(p => p.User.Id == targetPlayerId)
                ?? throw new InvalidOperationException("Player not found in this lobby.");

            Players.Remove(target);
            LobbySettings.UpdateTimestamp();
            return target;
        }
    }

    public bool IsPlayerInLobby(Guid id) => Players.Any(p => p.User.Id == id);

}
