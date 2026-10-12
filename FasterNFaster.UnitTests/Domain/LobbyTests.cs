using FasterNFaster.Api.Domain.Entities.Lobbies;
using FasterNFaster.Api.Domain.Entities.Lobbies.Colors;
using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;

namespace FasterNFaster.UnitTests.Domain;

public class LobbyTests
{
    private static readonly Func<Guid, bool> Claim = _ => true;

    [Fact]
    public void ClaimRefused_JoinThrowsAlreadyInLobbyAndAddsNoPlayer()
    {
        var lobby = CreateLobby();
        var joiner = Guid.NewGuid();

        Assert.Throws<AlreadyInLobbyException>(() => lobby.Join(joiner, "joiner", null, _ => false));

        Assert.False(lobby.IsPlayerIn(joiner));
        Assert.Empty(lobby.DrainEvents());
    }

    [Fact]
    public void FullLobby_JoinDoesNotClaim()
    {
        var lobby = CreateLobby();
        for (var i = 0; i < lobby.LobbySettings.MaxPlayers; i++)
            lobby.Join(Guid.NewGuid(), $"player{i}", null, Claim);
        var claimed = new List<Guid>();

        Assert.Throws<LobbyFullException>(() => lobby.Join(Guid.NewGuid(), "extra", null, id =>
        {
            claimed.Add(id);
            return true;
        }));

        Assert.Empty(claimed);
    }

    [Fact]
    public void PlayerAlreadyIn_JoinDoesNotClaim()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        var claimed = new List<Guid>();

        lobby.Join(first, "first", null, id =>
        {
            claimed.Add(id);
            return true;
        });

        Assert.Empty(claimed);
    }

    [Fact]
    public void AbsentPlayer_DisconnectReturnsFalseAndRaisesNothing()
    {
        var (lobby, _, _) = CreateLobbyWithTwoPlayers();
        lobby.DrainEvents();

        var disconnected = lobby.Disconnect(Guid.NewGuid());

        Assert.False(disconnected);
        Assert.Equal(2, lobby.Players.Count);
        Assert.Empty(lobby.DrainEvents());
    }

    [Fact]
    public void ChangePlayerColor_PaletteColorInAnyCase_StoresPaletteEntry()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        lobby.ChangePlayerColor(first, "#19D5FF");

        Assert.Equal(PlayerColors.Palette[2], ColorOf(lobby, first));
    }

    [Fact]
    public void ChangePlayerColor_CssUrl_ThrowsColorNotInPalette_AndKeepsColor()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        Assert.Throws<ColorNotInPaletteException>(() =>
            lobby.ChangePlayerColor(first, "url(https://example.com/x)"));

        Assert.Equal(PlayerColors.Palette[0], ColorOf(lobby, first));
    }

    [Theory]
    [InlineData("red")]
    [InlineData(" #19d5ff")]
    [InlineData("#19d5ff;background:url(https://example.com/x)")]
    public void ChangePlayerColor_NotAPaletteColor_ThrowsColorNotInPalette(string color)
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        Assert.Throws<ColorNotInPaletteException>(() => lobby.ChangePlayerColor(first, color));
    }

    [Fact]
    public void ChangePlayerColor_OwnCurrentColor_IsNoOp()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        var exception = Record.Exception(() => lobby.ChangePlayerColor(first, "#ff6b6b"));

        Assert.Null(exception);
        Assert.Equal(PlayerColors.Palette[0], ColorOf(lobby, first));
    }

    [Fact]
    public void ChangePlayerColor_ColorTakenByOtherPlayer_ThrowsColorIsAlreadyTaken()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        Assert.Throws<ColorIsAlreadyTakenException>(() => lobby.ChangePlayerColor(first, "#63F800"));

        Assert.Equal(PlayerColors.Palette[0], ColorOf(lobby, first));
    }

    [Fact]
    public void NonHostStartsSession_ShouldThrowNotHost()
    {
        var (lobby, _, second) = CreateLobbyWithTwoPlayers();

        var exception = Assert.Throws<NotHostException>(() => lobby.StartSession(second));

        Assert.Equal("Only the host can start the race", exception.Message);
    }

    [Fact]
    public void HostStartsSession_ShouldActivateSession()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        lobby.StartSession(first);

        Assert.True(lobby.IsSessionActive);
    }

    [Fact]
    public void HostStartsSessionTwice_ShouldThrowLobbyInRace()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        lobby.StartSession(first);

        Assert.Throws<LobbyInRaceException>(() => lobby.StartSession(first));
    }

    [Fact]
    public void NonHostTransfersHost_ShouldThrowNotHost()
    {
        var (lobby, _, second) = CreateLobbyWithTwoPlayers();

        Assert.Throws<NotHostException>(() => lobby.TransferHost(second, second));
    }

    [Fact]
    public void HostTransfersToSelf_ShouldThrowHostTransferToSelf()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        Assert.Throws<HostTransferToSelfException>(() => lobby.TransferHost(first, first));
    }

    [Fact]
    public void ChangeColorDuringRace_ShouldThrowLobbyInRace_AndKeepColor()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        lobby.StartSession(first);

        Assert.Throws<LobbyInRaceException>(() => lobby.ChangePlayerColor(first, "#19D5FF"));

        Assert.Equal(PlayerColors.Palette[0], ColorOf(lobby, first));
    }

    [Fact]
    public void NonHostKicksPlayer_ShouldThrowNotHost_AndKeepTarget()
    {
        var (lobby, _, second) = CreateLobbyWithTwoPlayers();

        Assert.Throws<NotHostException>(() => lobby.Kick(second, second));

        Assert.True(lobby.IsPlayerIn(second));
    }

    [Fact]
    public void HostKicksSelf_ThrowsHostKickSelf_AndKeepsHost()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        Assert.Throws<HostKickSelfException>(() => lobby.Kick(first, first));

        Assert.True(lobby.IsPlayerIn(first));
        Assert.Equal(first, lobby.HostId);
        Assert.False(lobby.IsPlayerBanned(first));
    }

    [Fact]
    public void KickDuringRace_ShouldThrowLobbyInRace_AndKeepTarget()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();
        lobby.StartSession(first);

        Assert.Throws<LobbyInRaceException>(() => lobby.Kick(first, second));

        Assert.True(lobby.IsPlayerIn(second));
    }

    [Fact]
    public void NonHostRefreshesPassage_ShouldThrowNotHost()
    {
        var (lobby, _, second) = CreateLobbyWithTwoPlayers();

        Assert.Throws<NotHostException>(() => lobby.EnsureCanRefreshPassage(second));
    }

    [Fact]
    public void RefreshPassageDuringRace_ShouldThrowLobbyInRace()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        lobby.StartSession(first);

        Assert.Throws<LobbyInRaceException>(() => lobby.EnsureCanRefreshPassage(first));
    }

    [Fact]
    public void HostRefreshesPassageWhileWaiting_ShouldNotThrow()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        var exception = Record.Exception(() => lobby.EnsureCanRefreshPassage(first));

        Assert.Null(exception);
    }

    [Fact]
    public void HostNeverJoined_JoinerBecomesHost()
    {
        var (lobby, _) = CreateLobbyWithUnjoinedHost();
        var joiner = Guid.NewGuid();

        lobby.Join(joiner, "joiner", null, Claim);

        Assert.Equal(joiner, lobby.HostId);
    }

    [Fact]
    public void HostNeverJoined_JoinerCanStartSession()
    {
        var (lobby, _) = CreateLobbyWithUnjoinedHost();
        var joiner = Guid.NewGuid();
        lobby.Join(joiner, "joiner", null, Claim);

        lobby.StartSession(joiner);

        Assert.True(lobby.IsSessionActive);
    }

    [Fact]
    public void CreatorJoinsFirst_CreatorStaysHost()
    {
        var (lobby, creator) = CreateLobbyWithUnjoinedHost();

        lobby.Join(creator, "creator", null, Claim);

        Assert.Equal(creator, lobby.HostId);
    }

    [Fact]
    public void HostIsMember_JoinerDoesNotBecomeHost()
    {
        var (lobby, creator) = CreateLobbyWithUnjoinedHost();
        lobby.Join(creator, "creator", null, Claim);

        lobby.Join(Guid.NewGuid(), "joiner", null, Claim);

        Assert.Equal(creator, lobby.HostId);
    }

    [Fact]
    public void HostNeverJoined_JoinRaisesNoHostChangedEvent()
    {
        var (lobby, _) = CreateLobbyWithUnjoinedHost();

        lobby.Join(Guid.NewGuid(), "joiner", null, Claim);

        Assert.DoesNotContain(lobby.DrainEvents(), e => e is HostChangedEvent);
    }

    [Fact]
    public void CreatorJoinsAfterTakeover_TakeoverHostStays()
    {
        var (lobby, creator) = CreateLobbyWithUnjoinedHost();
        var joiner = Guid.NewGuid();
        lobby.Join(joiner, "joiner", null, Claim);

        lobby.Join(creator, "creator", null, Claim);

        Assert.Equal(joiner, lobby.HostId);
    }

    [Fact]
    public void EmptyLobbyAtTtl_IsAbandoned()
    {
        var lobby = CreateLobby();
        var ttl = TimeSpan.FromMinutes(2);

        Assert.True(lobby.IsAbandoned(lobby.LobbySettings.CreatedAt + ttl, ttl));
    }

    [Fact]
    public void EmptyLobbyYoungerThanTtl_IsNotAbandoned()
    {
        var lobby = CreateLobby();
        var ttl = TimeSpan.FromMinutes(2);

        Assert.False(lobby.IsAbandoned(lobby.LobbySettings.CreatedAt + ttl - TimeSpan.FromTicks(1), ttl));
    }

    [Fact]
    public void OldLobbyWithPlayers_IsNotAbandoned()
    {
        var (lobby, _, _) = CreateLobbyWithTwoPlayers();
        var ttl = TimeSpan.FromMinutes(2);

        Assert.False(lobby.IsAbandoned(lobby.LobbySettings.CreatedAt + ttl, ttl));
    }

    [Fact]
    public void GetPlayerIds_ReturnsEveryJoinedPlayer()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();

        Assert.Equal([first, second], lobby.GetPlayerIds());
    }

    [Fact]
    public void GetPlayerIds_LaterJoin_DoesNotChangeReturnedList()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();
        var ids = lobby.GetPlayerIds();

        lobby.Join(Guid.NewGuid(), "third", null, Claim);

        Assert.Equal([first, second], ids);
    }

    [Fact]
    public void InviteCodesTaken_NewLobbyGetsFirstFreeCode()
    {
        List<string> tried = [];

        var lobby = new Lobby("Test", isPrivate: false, code =>
        {
            tried.Add(code);
            return tried.Count < 3;
        });

        Assert.Equal(3, tried.Count);
        Assert.Equal(tried[^1], lobby.LobbySettings.InviteCode);
    }

    [Fact]
    public void JoinPrivateLobbyWithoutCode_ThrowsInvalidInviteCode()
    {
        var lobby = CreatePrivateLobby();
        var joiner = Guid.NewGuid();

        Assert.Throws<InvalidInviteCodeException>(() => lobby.Join(joiner, "joiner", null, Claim));

        Assert.False(lobby.IsPlayerIn(joiner));
    }

    [Fact]
    public void JoinPrivateLobbyWithWrongCode_ThrowsInvalidInviteCode()
    {
        var lobby = CreatePrivateLobby();
        var joiner = Guid.NewGuid();

        Assert.Throws<InvalidInviteCodeException>(() => lobby.Join(joiner, "joiner", "wrong", Claim));

        Assert.False(lobby.IsPlayerIn(joiner));
    }

    [Fact]
    public void JoinPrivateLobbyWithLowercaseCode_AddsPlayer()
    {
        var lobby = CreatePrivateLobby();
        var joiner = Guid.NewGuid();

        lobby.Join(joiner, "joiner", lobby.LobbySettings.InviteCode.ToLowerInvariant(), Claim);

        Assert.True(lobby.IsPlayerIn(joiner));
    }

    [Fact]
    public void JoinPublicLobbyWithAnyCode_AddsPlayer()
    {
        var lobby = CreateLobby();
        var joiner = Guid.NewGuid();

        lobby.Join(joiner, "joiner", "anything", Claim);

        Assert.True(lobby.IsPlayerIn(joiner));
    }

    [Fact]
    public void KickedPlayerJoins_ThrowsPlayerBannedInLobby()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();
        lobby.Kick(first, second);

        Assert.Throws<PlayerBannedInLobbyException>(() => lobby.Join(second, "second", null, Claim));

        Assert.False(lobby.IsPlayerIn(second));
    }

    [Fact]
    public void JoinFullLobby_ThrowsLobbyFull()
    {
        var lobby = CreateLobby();
        for (var i = 0; i < lobby.LobbySettings.MaxPlayers; i++)
            lobby.Join(Guid.NewGuid(), $"player{i}", null, Claim);

        Assert.Throws<LobbyFullException>(() => lobby.Join(Guid.NewGuid(), "extra", null, Claim));

        Assert.Equal(lobby.LobbySettings.MaxPlayers, lobby.Players.Count);
    }

    [Fact]
    public void JoinDuringSession_ThrowsLobbyIsNotAcceptingPlayers()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        lobby.StartSession(first);
        var joiner = Guid.NewGuid();

        Assert.Throws<LobbyIsNotAcceptingPlayersException>(() => lobby.Join(joiner, "joiner", null, Claim));

        Assert.False(lobby.IsPlayerIn(joiner));
    }

    [Fact]
    public void PlayerAlreadyInJoins_IsNoOp()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        lobby.DrainEvents();

        lobby.Join(first, "first", null, Claim);

        Assert.Equal(2, lobby.Players.Count);
        Assert.Empty(lobby.DrainEvents());
    }

    [Fact]
    public void NewPlayerJoins_GetsNextJoinOrderAndFirstFreeColor()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        lobby.ChangePlayerColor(first, PlayerColors.Palette[2]);
        var joiner = Guid.NewGuid();

        lobby.Join(joiner, "joiner", null, Claim);

        var player = lobby.Players.Single(p => p.Id == joiner);
        Assert.Equal(3, player.JoinOrder);
        Assert.Equal(PlayerColors.Palette[0], player.Color);
    }

    [Fact]
    public void NewPlayerJoins_RaisesPlayerJoinedEvent()
    {
        var lobby = CreateLobby();
        var joiner = Guid.NewGuid();

        lobby.Join(joiner, "joiner", null, Claim);

        var joined = Assert.Single(lobby.DrainEvents().OfType<PlayerJoinedEvent>());
        Assert.Equal(joiner, joined.UserId);
        Assert.Equal(lobby.Id, joined.LobbyId);
        Assert.Equal("joiner", joined.Nick);
    }

    [Fact]
    public void HostKicksPlayer_RemovesAndBansTarget()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();

        var kicked = lobby.Kick(first, second);

        Assert.Equal(second, kicked.Id);
        Assert.False(lobby.IsPlayerIn(second));
        Assert.True(lobby.IsPlayerBanned(second));
    }

    [Fact]
    public void HostKicksPlayer_RaisesPlayerKickedEvent()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();
        lobby.DrainEvents();

        lobby.Kick(first, second);

        var kicked = Assert.Single(lobby.DrainEvents().OfType<PlayerKickedEvent>());
        Assert.Equal(second, kicked.UserId);
        Assert.Equal(lobby.Id, kicked.LobbyId);
        Assert.Equal("second", kicked.Nick);
    }

    [Fact]
    public void HostTransfersToMember_ChangesHostAndRaisesHostChanged()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();
        lobby.DrainEvents();

        lobby.TransferHost(first, second);

        Assert.Equal(second, lobby.HostId);
        var changed = Assert.Single(lobby.DrainEvents().OfType<HostChangedEvent>());
        Assert.Equal(lobby.Id, changed.LobbyId);
        Assert.Equal(second, changed.NewHostId);
        Assert.Equal("second", changed.NewHostNick);
    }

    [Fact]
    public void HostTransfersToPlayerNotInLobby_ThrowsInvalidOperation_AndKeepsHost()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        Assert.Throws<InvalidOperationException>(() => lobby.TransferHost(first, Guid.NewGuid()));

        Assert.Equal(first, lobby.HostId);
    }

    [Fact]
    public void ColorChangedToFreeColor_ReleasesPreviousColor()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();

        lobby.ChangePlayerColor(first, PlayerColors.Palette[2]);

        Assert.Equal(PlayerColors.Palette[2], ColorOf(lobby, first));
        Assert.Contains(lobby.GetColors(), c => c.Color == PlayerColors.Palette[0] && c.IsAvailable);
        Assert.Contains(lobby.GetColors(), c => c.Color == PlayerColors.Palette[2] && !c.IsAvailable);
    }

    [Fact]
    public void HostDisconnects_PromotesLowestJoinOrder()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        var third = Guid.NewGuid();
        lobby.Join(third, "third", null, Claim);
        lobby.TransferHost(first, third);

        lobby.Disconnect(third);

        Assert.Equal(first, lobby.HostId);
    }

    [Fact]
    public void HostDisconnects_RaisesHostChangedEvent()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();
        lobby.DrainEvents();

        lobby.Disconnect(first);

        var changed = Assert.Single(lobby.DrainEvents().OfType<HostChangedEvent>());
        Assert.Equal(second, changed.NewHostId);
        Assert.Equal("second", changed.NewHostNick);
    }

    [Fact]
    public void NonHostDisconnects_KeepsHostAndRaisesNoHostChanged()
    {
        var (lobby, first, second) = CreateLobbyWithTwoPlayers();
        lobby.DrainEvents();

        lobby.Disconnect(second);

        Assert.Equal(first, lobby.HostId);
        Assert.DoesNotContain(lobby.DrainEvents(), e => e is HostChangedEvent);
    }

    [Fact]
    public void LastPlayerDisconnects_LeavesLobbyEmptyWithoutHostChanged()
    {
        var lobby = CreateLobby();
        var only = Guid.NewGuid();
        lobby.Join(only, "only", null, Claim);
        lobby.DrainEvents();

        lobby.Disconnect(only);

        Assert.True(lobby.IsEmpty());
        Assert.DoesNotContain(lobby.DrainEvents(), e => e is HostChangedEvent);
    }

    [Fact]
    public void HostStartsSession_RaisesSessionStartedEvent()
    {
        var (lobby, first, _) = CreateLobbyWithTwoPlayers();
        lobby.DrainEvents();

        lobby.StartSession(first);

        var started = Assert.Single(lobby.DrainEvents().OfType<SessionStartedEvent>());
        Assert.Equal(lobby.Id, started.LobbyId);
    }

    [Fact]
    public void HostStartsSessionWithNoPlayers_ThrowsInvalidOperation()
    {
        var (lobby, creator) = CreateLobbyWithUnjoinedHost();

        Assert.Throws<InvalidOperationException>(() => lobby.StartSession(creator));

        Assert.False(lobby.IsSessionActive);
    }

    private static Lobby CreateLobby() => new("Test", isPrivate: false, inviteCodeExists: _ => false);

    private static Lobby CreatePrivateLobby() => new("Test", isPrivate: true, inviteCodeExists: _ => false);

    private static (Lobby Lobby, Guid Creator) CreateLobbyWithUnjoinedHost()
    {
        var lobby = CreateLobby();
        var creator = Guid.NewGuid();
        lobby.AssignHost(creator);

        return (lobby, creator);
    }

    private static (Lobby Lobby, Guid First, Guid Second) CreateLobbyWithTwoPlayers()
    {
        var lobby = CreateLobby();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        lobby.Join(first, "first", null, Claim);
        lobby.AssignHost(first);
        lobby.Join(second, "second", null, Claim);

        return (lobby, first, second);
    }

    private static string ColorOf(Lobby lobby, Guid playerId) =>
        lobby.Players.Single(p => p.Id == playerId).Color;
}
