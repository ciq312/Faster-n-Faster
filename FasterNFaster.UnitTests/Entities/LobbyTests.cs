using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
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
