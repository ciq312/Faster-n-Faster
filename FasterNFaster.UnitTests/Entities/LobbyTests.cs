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
