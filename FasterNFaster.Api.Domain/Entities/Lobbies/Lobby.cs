using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.Core.Exceptions.Lobbies;

namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class Lobby : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public Guid HostId { get; private set; }
    public LobbySettings LobbySettings { get; private set; }
    public bool IsSessionActive { get; private set; } = false;
    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();
    private List<Guid> bannedPlayerIds = new List<Guid>();

    public Lobby(string name, bool isPrivate)
    {
        Id = Guid.NewGuid();
        Name = name;
        LobbySettings = new LobbySettings(isPrivate);
    }
    public void StartSession()
    {
        if (IsSessionActive) throw new InvalidOperationException("Session is already active.");
        if (Players.Count == 0) throw new InvalidOperationException("Can't start session with no players");

        IsSessionActive = true;
        RaiseDomainEvent(new SessionStartedEvent(Id));
    }

    public List<RaceParticipant> GetRaceParticipants() => Players.Select(x => new RaceParticipant(x.Id, x.Color, x.Nick)).ToList();

    public void EndSession()
    {
        if (!IsSessionActive) throw new InvalidOperationException("Session is already not active");

        IsSessionActive = false;
    }

    public void Join(Guid userId, string nick, string? code)
    {
        if (IsPlayerIn(userId)) return;
        if (!IsCodeCorrect(code, LobbySettings.InviteCode) && LobbySettings.IsPrivate) throw new InvalidInviteCodeException();
        if (IsPlayerBanned(userId)) throw new PlayerBannedInLobbyException();

        AddPlayer(userId, nick);
    }
    private bool IsCodeCorrect(string? codeToCheck, string? actualCode) => string.Equals(codeToCheck, actualCode, StringComparison.OrdinalIgnoreCase);

    private void AddPlayer(Guid userId, string nick)
    {
        if (IsSessionActive)
            throw new LobbyIsNotAcceptingPlayersException();

        if (Players.Count >= LobbySettings.MaxPlayers)
            throw new LobbyFullException();

        var joinOrder = Players.Count != 0 ? Players.Max(p => p.JoinOrder) + 1 : 1;
        var color = PlayerColors.GetFirstAvailableFromPalette(Players.Select(p => p.Color));
        var player = new LobbyPlayer(userId, nick, joinOrder, color);
        Players.Add(player);
        LobbySettings.UpdateTimestamp();
        RaiseDomainEvent(new PlayerJoinedEvent(userId, Id, nick));
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
        ValidateHost(hostId);

        if (hostId == newHostId)
            throw new InvalidOperationException("Cannot transfer host to yourself.");

        var target =
            Players.FirstOrDefault(p => p.Id == newHostId)
            ?? throw new InvalidOperationException(
                "Target player is not in this lobby or is disconnected."
            );

        AssignHost(newHostId);
        RaiseDomainEvent(new HostChangedEvent(Id, target.Id, target.Nick));
    }

    public void ChangePlayerColor(Guid playerId, string newColor)
    {
        if (IsSessionActive)
            throw new InvalidOperationException("Can only change color while waiting.");

        if (Players.Any(p => p.Color == newColor))
            throw new ColorIsAlreadyTakenException();

        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new InvalidOperationException("Player not found in this lobby.");

        player.ChangeColor(newColor);
        LobbySettings.UpdateTimestamp();
    }

    public LobbyPlayer Kick(Guid initiatorId, Guid targetPlayerId)
    {
        ValidateHost(initiatorId);
        if (IsSessionActive) throw new InvalidOperationException("Can't kick when racing");

        var kickedPlayer = RemovePlayerInternal(targetPlayerId);
        BanPlayer(kickedPlayer.Id);
        RaiseDomainEvent(new PlayerKickedEvent(kickedPlayer.Id, Id, kickedPlayer.Nick));
        return kickedPlayer;
    }

    public LobbyPlayer Disconnect(Guid playerId)
    {
        var player = RemovePlayerInternal(playerId);
        RaiseDomainEvent(new PlayerDisconnectedEvent(playerId, Id, player.Nick));
        return player;
    }
    
    private LobbyPlayer RemovePlayerInternal(Guid playerId)
    {
        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new InvalidOperationException("Player not found in this lobby.");

        Players.Remove(player);
        PromoteNextIfHost(playerId);
        RaiseDomainEvent(new PlayerRemovedEvent(player.Id, Id, player.Nick));
        LobbySettings.UpdateTimestamp();
        return player;
    }




    private void PromoteNextIfHost(Guid leavingPlayerId)
    {
        if (HostId != leavingPlayerId) return;

        var newHost = Players
           .OrderBy(p => p.JoinOrder)
           .FirstOrDefault();

        if (newHost == null) return;

        AssignHost(newHost.Id);
        RaiseDomainEvent(new HostChangedEvent(Id, newHost.Id, newHost.Nick));
    }

    public void BanPlayer(Guid userId) => bannedPlayerIds.Add(userId);

    public void GenerateUniqueInviteCode(Func<string, bool> codeExists)
    {
        string code = LobbySettings.CreateUniqueInviteCode(codeExists);
        LobbySettings.SetInviteCode(code);
    }

    public bool IsPlayerIn(Guid userId) => Players.Any(p => p.Id == userId);

    public bool IsEmpty() => Players.Count == 0;

    public IEnumerable<ColorStatus> GetColors()
        => PlayerColors.Palette.Select(c => new ColorStatus(c, !Players.Any(p => p.Color == c)));

    public bool IsPlayerBanned(Guid id) => bannedPlayerIds.Contains(id);

    public bool IsPlayerHost(Guid id) => id == HostId;

}
