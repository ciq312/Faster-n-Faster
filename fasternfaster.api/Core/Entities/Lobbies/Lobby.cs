using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Lobbies.Colors;
using FasterNFaster.Api.Core.Lobbies.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class Lobby(string name, bool isPrivate, WordRace race) : AggregateRoot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = name;
    public Guid HostId { get; private set; }
    public LobbySettings LobbySettings { get; private set; } = new LobbySettings(isPrivate);
    public bool IsSessionActive { get; private set; } = false;
    public WordRace Race { get; private set; } = race;
    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();


    private readonly object _lock = new();

    public void StartSession()
    {
        lock (_lock)
        {
            if (IsSessionActive) throw new InvalidOperationException("Session is already active.");
            if (Players.Count == 0) throw new InvalidOperationException("Can't start session with no players");

            Race.Start(Players.Select(p => (p.User.Id, p.Color, p.User.Nick)));
            IsSessionActive = true;
        }
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

    public LobbyPlayer? TryPromoteNextHost(Guid leavingPlayerId)
    {
        lock (_lock)
        {
            if (HostId != leavingPlayerId) return null;

            var nextHost = Players
                .Where(p => p.IsConnected)
                .OrderBy(p => p.JoinOrder)
                .FirstOrDefault();

            if (nextHost == null) return null;

            AssignHost(nextHost.User.Id);
            RaiseDomainEvent(new HostChangedEvent(Id, nextHost.User.Id, nextHost.User.Nick));
            return nextHost;
        }
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
            RaiseDomainEvent(new HostChangedEvent(Id, target.User.Id, target.User.Nick));
        }
    }

    public void KickPlayer(Guid hostId, Guid targetPlayerId)
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
            RaiseDomainEvent(new PlayerKickedEvent(target.User.Id, Id, target.User.Nick));
            LobbySettings.UpdateTimestamp();
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
            RaiseDomainEvent(new PlayerDisconnectedEvent(player.User.Id, Id, player.User.Nick));
            LobbySettings.UpdateTimestamp();
        }
    }
    public async Task GenerateUniqueInviteCode(Func<string, bool> codeExists)
    {
        string code = LobbySettings.CreateUniqueInviteCode(codeExists);

        LobbySettings.SetInviteCode(code);
    }
    public bool IsPlayerInLobby(Guid id)
    {

        lock (_lock) { return Players.Any(p => p.User.Id == id); }
    }

    public IEnumerable<ColorStatus> GetColors()
    => PlayerColors.Palette.Select(c => new
    ColorStatus(c, !Players.Any(p => p.Color == c)));

}
