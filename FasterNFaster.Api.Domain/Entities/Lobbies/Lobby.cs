using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class Lobby : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public Guid HostId { get; private set; }
    public LobbySettings LobbySettings { get; private set; }
    public bool IsSessionActive { get; private set; } = false;
    private readonly List<LobbyPlayer> players = [];
    public IReadOnlyList<LobbyPlayer> Players => players;

    private readonly List<Guid> bannedPlayerIds = new List<Guid>();

    public Lobby(string name, bool isPrivate)
    {
        Id = Guid.NewGuid();
        Name = name;
        LobbySettings = new LobbySettings(isPrivate);
    }
    public void StartSession(Guid initiatorId)
    {
        EnsureHost(initiatorId, "start the race");
        if (IsSessionActive) throw new LobbyInRaceException("start the race");
        if (Players.Count == 0) throw new InvalidOperationException("Can't start session with no players");

        IsSessionActive = true;
        RaiseDomainEvent(new SessionStartedEvent(Id));
    }

    public List<RaceParticipant> GetRaceParticipants(IAntiCheatPolicy policy) =>
        Players.Select(x => new RaceParticipant(x.Id, x.Color, x.Nick, policy)).ToList();

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
        players.Add(player);
        ClaimHostIfVacant(userId);
        LobbySettings.UpdateTimestamp();
        RaiseDomainEvent(new PlayerJoinedEvent(userId, Id, nick));
    }

    public void AssignHost(Guid hostId)
    {
        HostId = hostId;
        LobbySettings.UpdateTimestamp();
    }

    private void EnsureHost(Guid userId, string action)
    {
        if (!IsPlayerHost(userId))
            throw new NotHostException(action);
    }

    public void TransferHost(Guid hostId, Guid newHostId)
    {
        EnsureHost(hostId, "promote a player");

        if (hostId == newHostId)
            throw new HostTransferToSelfException();

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
            throw new LobbyInRaceException("change color");

        var color = PlayerColors.FindInPalette(newColor) ?? throw new ColorNotInPaletteException();

        var player = Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new InvalidOperationException("Player not found in this lobby.");

        if (player.Color == color) return;

        if (Players.Any(p => p.Color == color))
            throw new ColorIsAlreadyTakenException();

        player.ChangeColor(color);
        LobbySettings.UpdateTimestamp();
    }

    public LobbyPlayer Kick(Guid initiatorId, Guid targetPlayerId)
    {
        const string action = "kick players";
        EnsureHost(initiatorId, action);
        if (IsSessionActive) throw new LobbyInRaceException(action);

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

        players.Remove(player);
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

    private void ClaimHostIfVacant(Guid joinerId)
    {
        if (!IsPlayerIn(HostId)) AssignHost(joinerId);
    }

    public void BanPlayer(Guid userId) => bannedPlayerIds.Add(userId);

    public void GenerateUniqueInviteCode(Func<string, bool> codeExists)
    {
        var code = LobbySettings.CreateUniqueInviteCode(codeExists);
        LobbySettings.SetInviteCode(code);
    }

    public void EnsureCanRefreshPassage(Guid callerId)
    {
        const string action = "change the passage";
        EnsureHost(callerId, action);
        if (IsSessionActive) throw new LobbyInRaceException(action);
    }

    public bool IsPlayerIn(Guid userId) => Players.Any(p => p.Id == userId);

    public bool IsEmpty() => Players.Count == 0;

    public bool IsAbandoned(DateTime now, TimeSpan emptyLobbyTtl) =>
        IsEmpty() && now - LobbySettings.CreatedAt >= emptyLobbyTtl;

    public IEnumerable<ColorStatus> GetColors()
        => PlayerColors.Palette.Select(c => new ColorStatus(c, !Players.Any(p => p.Color == c)));

    public bool IsPlayerBanned(Guid id) => bannedPlayerIds.Contains(id);

    public bool IsPlayerHost(Guid id) => id == HostId;

}
