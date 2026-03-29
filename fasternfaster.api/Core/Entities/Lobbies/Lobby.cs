using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Lobbies.Colors;

namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class Lobby
{
    public enum Status
    {
        waiting,
        racing,
    }

    private static readonly Dictionary<Status, Status> AllowedTransitions = new()
    {
        { Status.waiting, Status.racing },
        { Status.racing, Status.waiting },
    };

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Status CurrentStatus { get; private set; }
    public Guid HostId { get; private set; }
    public LobbySettings LobbySettings { get; private set; }
    public RaceSettings RaceSettings { get; private set; } = new();
    public Race Race { get; private set; }
    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();
    public ICollection<RaceParticipantResult> RaceStatics { get; private set; } = new List<RaceParticipantResult>();

    public Lobby(string name, bool isPrivate)
    {
        Id = Guid.NewGuid();
        Name = name;
        LobbySettings = new LobbySettings(isPrivate);
        CurrentStatus = Status.waiting;
        Race = RaceSettings.BuildRace();
    }

    public void UpdateRaceSettings(Guid hostId, Action<RaceSettings> configure)
    {
        lock (_lock)
        {
            ValidateHost(hostId);

            if (CurrentStatus != Status.waiting)
                throw new InvalidOperationException("Can only change settings while waiting.");

            configure(RaceSettings);
            Race = RaceSettings.BuildRace();
            LobbySettings.UpdateTimestamp();
        }
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
            var color = PlayerColors.GetFirstAvailableFromPalette(Players.Select(p => p.Color));
            var player = new LobbyPlayer(user, this, joinOrder, color);
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
            TransitionStatus(Status.racing);

            Race = RaceSettings.BuildRace();

            var connectedPlayers = Players
                .Where(p => p.IsConnected)
                .Select(p => (p.User.Id, p.Color, p.User.Nick));

            Race.Start(connectedPlayers);
        }
    }

    /// <summary>
    /// Thread-safe finish — only the first caller succeeds.
    /// Returns the finished race so the caller can read results.
    /// </summary>
    public Race? TryFinishRace()
    {
        lock (_lock)
        {
            if (CurrentStatus != Status.racing) return null;
            TransitionStatus(Status.waiting);
            var finishedRace = Race;
            Race = RaceSettings.BuildRace();
            return finishedRace;
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

    public void ChangePlayerColor(Guid playerId, string newColor)
    {
        lock (_lock)
        {
            if (CurrentStatus != Status.waiting)
                throw new InvalidOperationException("Can only change color while waiting.");

            if (Players.Any(p => p.Color == newColor))
                throw new InvalidOperationException("Color is already taken.");

            var player = Players.FirstOrDefault(p => p.User.Id == playerId)
                ?? throw new InvalidOperationException("Player not found in this lobby.");

            player.ChangeColor(newColor);
            LobbySettings.UpdateTimestamp();
        }
    }

    public bool IsPlayerInLobby(Guid id) => Players.Any(p => p.User.Id == id);

    public IEnumerable<ColorStatus> GetColors()
    => PlayerColors.Palette.Select(c => new
    ColorStatus(c, !Players.Any(p => p.Color == c)));

}
