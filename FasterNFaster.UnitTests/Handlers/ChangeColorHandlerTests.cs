using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

namespace FasterNFaster.UnitTests.Handlers;

public class ChangeColorHandlerTests
{
    [Fact]
    public async Task FreeColor_ChangesCallersColor()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        await ChangeColor(context, other.Id, PlayerColors.Palette[2]);

        Assert.Equal(PlayerColors.Palette[2], ColorOf(context, other.Id));
    }

    [Fact]
    public async Task ColorTakenByOther_ThrowsColorIsAlreadyTaken()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();
        var colorBefore = ColorOf(context, other.Id);

        await Assert.ThrowsAsync<ColorIsAlreadyTakenException>(() => ChangeColor(context, other.Id, PlayerColors.Palette[0]));

        Assert.Equal(colorBefore, ColorOf(context, other.Id));
    }

    [Fact]
    public async Task DuringRace_ThrowsLobbyInRace()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);

        await Assert.ThrowsAsync<LobbyInRaceException>(() => ChangeColor(context, other.Id, PlayerColors.Palette[2]));
    }

    [Fact]
    public async Task CallerNotInLobby_ThrowsUserNotFound()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<UserNotFoundException>(() => ChangeColor(context, Guid.NewGuid(), PlayerColors.Palette[2]));
    }

    private static Task ChangeColor(LobbyTestContext context, Guid callerId, string color) =>
        new ChangeColorHandler(context.LobbyAccess)
            .Handle(new ChangeColorCommand(callerId, color), CancellationToken.None);

    private static string ColorOf(LobbyTestContext context, Guid playerId) =>
        context.Lobby.Players.Single(p => p.Id == playerId).Color;
}
