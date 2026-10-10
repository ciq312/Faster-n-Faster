using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Exceptions.Lobbies;

namespace FasterNFaster.Tests.Entities;

public class LobbyTests
{
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

        lobby.Join(joiner, "joiner", null);

        Assert.Equal(joiner, lobby.HostId);
    }

    [Fact]
    public void HostNeverJoined_JoinerCanStartSession()
    {
        var (lobby, _) = CreateLobbyWithUnjoinedHost();
        var joiner = Guid.NewGuid();
        lobby.Join(joiner, "joiner", null);

        lobby.StartSession(joiner);

        Assert.True(lobby.IsSessionActive);
    }

    [Fact]
    public void CreatorJoinsFirst_CreatorStaysHost()
    {
        var (lobby, creator) = CreateLobbyWithUnjoinedHost();

        lobby.Join(creator, "creator", null);

        Assert.Equal(creator, lobby.HostId);
    }

    [Fact]
    public void HostIsMember_JoinerDoesNotBecomeHost()
    {
        var (lobby, creator) = CreateLobbyWithUnjoinedHost();
        lobby.Join(creator, "creator", null);

        lobby.Join(Guid.NewGuid(), "joiner", null);

        Assert.Equal(creator, lobby.HostId);
    }

    [Fact]
    public void HostNeverJoined_JoinRaisesNoHostChangedEvent()
    {
        var (lobby, _) = CreateLobbyWithUnjoinedHost();

        lobby.Join(Guid.NewGuid(), "joiner", null);

        Assert.DoesNotContain(lobby.DrainEvents(), e => e is HostChangedEvent);
    }

    [Fact]
    public void CreatorJoinsAfterTakeover_TakeoverHostStays()
    {
        var (lobby, creator) = CreateLobbyWithUnjoinedHost();
        var joiner = Guid.NewGuid();
        lobby.Join(joiner, "joiner", null);

        lobby.Join(creator, "creator", null);

        Assert.Equal(joiner, lobby.HostId);
    }

    [Fact]
    public void EmptyLobbyAtTtl_IsAbandoned()
    {
        var lobby = new Lobby("Test", isPrivate: false);
        var ttl = TimeSpan.FromMinutes(2);

        Assert.True(lobby.IsAbandoned(lobby.LobbySettings.CreatedAt + ttl, ttl));
    }

    [Fact]
    public void EmptyLobbyYoungerThanTtl_IsNotAbandoned()
    {
        var lobby = new Lobby("Test", isPrivate: false);
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

    private static (Lobby Lobby, Guid Creator) CreateLobbyWithUnjoinedHost()
    {
        var lobby = new Lobby("Test", isPrivate: false);
        var creator = Guid.NewGuid();
        lobby.AssignHost(creator);

        return (lobby, creator);
    }

    private static (Lobby Lobby, Guid First, Guid Second) CreateLobbyWithTwoPlayers()
    {
        var lobby = new Lobby("Test", isPrivate: false);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        lobby.Join(first, "first", null);
        lobby.AssignHost(first);
        lobby.Join(second, "second", null);

        return (lobby, first, second);
    }

    private static string ColorOf(Lobby lobby, Guid playerId) =>
        lobby.Players.Single(p => p.Id == playerId).Color;
}
