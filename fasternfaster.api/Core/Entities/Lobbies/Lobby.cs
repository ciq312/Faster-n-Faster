using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
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
    public List<Guid> BannedPlayersIds = new List<Guid>();

    public void InitializeSession()
    {
        if (IsSessionActive) throw new InvalidOperationException("Session is already active.");
        if (Players.Count == 0) throw new InvalidOperationException("Can't start session with no players");

        Race.Reset();
        IsSessionActive = true;
    }

    public void LaunchSession()
    {
        if (!IsSessionActive) throw new InvalidOperationException("Session is not active.");
        if (Race.HasStarted) throw new InvalidOperationException("Race already started.");

        foreach (var player in Players)
            Race.AddParticipant(new RaceParticipant(player.User.Id, player.Color, player.User.Nick));

        Race.Start();
    }

    public void OnSessionEnded()
    {
        if (!IsSessionActive) throw new InvalidOperationException("Session is already not active");

        IsSessionActive = false;
    }

    public void Join(User user, string? code)
    {
        if (IsPlayerInLobby(user.Id)) return;
        if (!IsCodeCorrect(code, LobbySettings.InviteCode) && isPrivate) throw new InvalidInviteCodeException();
        if (IsPlayerBanned(user.Id)) throw new PlayerBannedInLobbyException();

        AddPlayer(user);
    }
    private bool IsCodeCorrect(string? codeToCheck, string? actualCode) => string.Equals(codeToCheck, actualCode, StringComparison.OrdinalIgnoreCase);

    private void AddPlayer(User user)
    {
        if (IsSessionActive)
            throw new LobbyIsNotAcceptingPlayersException();

        if (Players.Count >= LobbySettings.MaxPlayers)
            throw new LobbyFullException();

        var joinOrder = Players.Count != 0 ? Players.Max(p => p.JoinOrder) + 1 : 1;
        var color = PlayerColors.GetFirstAvailableFromPalette(Players.Select(p => p.Color));
        var player = new LobbyPlayer(user, this, joinOrder, color);
        Players.Add(player);
        LobbySettings.UpdateTimestamp();
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
            Players.FirstOrDefault(p => p.User.Id == newHostId && p.IsConnected)
            ?? throw new InvalidOperationException(
                "Target player is not in this lobby or is disconnected."
            );

        AssignHost(newHostId);
        RaiseDomainEvent(new HostChangedEvent(Id, target.User.Id, target.User.Nick));
    }

    public void ChangePlayerColor(Guid playerId, string newColor)
    {
        if (IsSessionActive)
            throw new InvalidOperationException("Can only change color while waiting.");

        if (Players.Any(p => p.Color == newColor))
            throw new ColorIsAlreadyTakenException();

        var player = Players.FirstOrDefault(p => p.User.Id == playerId)
            ?? throw new InvalidOperationException("Player not found in this lobby.");

        player.ChangeColor(newColor);
        LobbySettings.UpdateTimestamp();
    }

    public LobbyPlayer RemovePlayer(Guid playerId)
    {
        var player = Players.FirstOrDefault(p => p.User.Id == playerId)
            ?? throw new InvalidOperationException("Player not found in this lobby.");

        if (IsSessionActive)
            Race.WithdrawParticipant(playerId);

        Players.Remove(player);
        PromoteNextIfHost(playerId);
        RaiseDomainEvent(new PlayerRemovedEvent(player.User.Id, Id, player.User.Nick));
        LobbySettings.UpdateTimestamp();
        return player;
    }

    private void PromoteNextIfHost(Guid leavingPlayerId)
    {
        if (HostId != leavingPlayerId) return;

        var newHost = Players
           .Where(p => p.IsConnected)
           .OrderBy(p => p.JoinOrder)
           .FirstOrDefault();

        if (newHost == null) return;

        AssignHost(newHost.User.Id);
        RaiseDomainEvent(new HostChangedEvent(Id, newHost.User.Id, newHost.User.Nick));
    }

    public void BanPlayer(Guid id) => BannedPlayersIds.Add(id);

    public async Task GenerateUniqueInviteCode(Func<string, bool> codeExists)
    {
        string code = LobbySettings.CreateUniqueInviteCode(codeExists);

        LobbySettings.SetInviteCode(code);
    }

    public bool IsPlayerInLobby(Guid id) => Players.Any(p => p.User.Id == id);

    public bool IsEmpty() => Players.Count == 0;

    public IEnumerable<ColorStatus> GetColors()
    => PlayerColors.Palette.Select(c => new
    ColorStatus(c, !Players.Any(p => p.Color == c)));

    public bool IsPlayerBanned(Guid id) => BannedPlayersIds.Contains(id);

}
