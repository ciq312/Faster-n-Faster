using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Lobbies.Colors;

namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class Lobby(string name, bool isPrivate, WordRace race)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = name;
    public Guid HostId { get; private set; }
    public LobbySettings LobbySettings { get; private set; } = new LobbySettings(isPrivate);
    public bool IsSessionActive { get; private set; } = false;
    // public RaceSettings RaceSettings { get; private set; } = new();
    public WordRace Race { get; private set; } = race;
    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();

    // public void SetInitialPassage(string passage)
    // {
    //     RaceSettings.SetPassage(passage);
    //     Race = RaceSettings.BuildRace();
    // }

    // public void RefreshPassage(Guid hostId, string passage)
    // {
    //     lock (_lock)
    //     {
    //         ValidateHost(hostId);

    //         if (CurrentStatus != Status.waiting)
    //             throw new InvalidOperationException("Can only change passage while waiting.");

    //         RaceSettings.SetPassage(passage);
    //         Race = RaceSettings.BuildRace();
    //         LobbySettings.UpdateTimestamp();
    //     }
    // }


    // public void UpdateRaceSettings(Guid hostId, Action<RaceSettings> configure)
    // {
    //     lock (_lock)
    //     {
    //         ValidateHost(hostId);

    //         if (CurrentStatus != Status.waiting)
    //             throw new InvalidOperationException("Can only change settings while waiting.");

    //         configure(RaceSettings);
    //         Race = RaceSettings.BuildRace();
    //         LobbySettings.UpdateTimestamp();
    //     }
    // }

    // public void TransitionStatus(Status newStatus)
    // {
    //     if (
    //         !AllowedTransitions.TryGetValue(CurrentStatus, out var expected)
    //         || expected != newStatus
    //     )
    //         throw new InvalidOperationException(
    //             $"Cannot transition from '{CurrentStatus}' to '{newStatus}'."
    //         );

    //     CurrentStatus = newStatus;
    //     LobbySettings.UpdateTimestamp();
    // }

    private readonly object _lock = new();

    public void StartSession()
    {
        if (IsSessionActive) throw new InvalidOperationException("Session is already active.");
        if (Players.Count == 0) throw new InvalidOperationException("Can't start session with no players");

        Race.Start(Players.Select(p => (p.User.Id, p.Color, p.User.Nick)));
        IsSessionActive = true;
    }

    public void OnSessionEnded()
    {
        if (!IsSessionActive) throw new InvalidOperationException("Session is already not active");
        IsSessionActive = false;
    }
    public LobbyPlayer AddPlayer(User user)
    {
        lock (_lock)
        {
            if (IsSessionActive)
                throw new InvalidOperationException("Lobby is not accepting players.");

            if (Players.Count >= LobbySettings.MaxPlayers)
                throw new InvalidOperationException("Lobby is full.");

            var joinOrder = Players.Count != 0 ? Players.Max(p => p.JoinOrder) + 1 : 1;
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

    // public void StartRace(Guid hostId)
    // {
    //     lock (_lock)
    //     {
    //         ValidateHost(hostId);
    //         TransitionStatus(Status.racing);

    //         Race = RaceSettings.BuildRace();

    //         var connectedPlayers = Players
    //             .Where(p => p.IsConnected)
    //             .Select(p => (p.User.Id, p.Color, p.User.Nick));

    //         Race.Start(connectedPlayers);
    //     }
    // }

    /// <summary>
    /// Returns the finished race so the caller can read results.
    /// </summary>
    // public Race? TryFinishRace()
    // {
    //     lock (_lock)
    //     {
    //         if (CurrentStatus != Status.racing) return null;
    //         if (!Race.IsRaceOver()) return null;
    //         TransitionStatus(Status.waiting);
    //         var finishedRace = Race;
    //         return finishedRace;
    //     }
    // }
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
            if (IsSessionActive)
                throw new InvalidOperationException("Can only change color while waiting.");

            if (Players.Any(p => p.Color == newColor))
                throw new InvalidOperationException("Color is already taken.");

            var player = Players.FirstOrDefault(p => p.User.Id == playerId)
                ?? throw new InvalidOperationException("Player not found in this lobby.");

            player.ChangeColor(newColor);
            LobbySettings.UpdateTimestamp();
        }
    }

    public void RemovePlayer(Guid playerId)
    {
        lock (_lock)
        {
            var player = Players.FirstOrDefault(p => p.User.Id == playerId)
                ?? throw new InvalidOperationException("Player not found in this lobby.");

            Players.Remove(player);
            LobbySettings.UpdateTimestamp();
        }
    }

    public bool IsPlayerInLobby(Guid id) => Players.Any(p => p.User.Id == id);

    public IEnumerable<ColorStatus> GetColors()
    => PlayerColors.Palette.Select(c => new
    ColorStatus(c, !Players.Any(p => p.Color == c)));

}
