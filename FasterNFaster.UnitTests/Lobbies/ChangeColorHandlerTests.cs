using FasterNFaster.Api.Domain.Entities.Lobbies.Colors;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.ChangeColor;
using FasterNFaster.Api.UseCases.Lobbies.Exceptions;

namespace FasterNFaster.UnitTests.Lobbies;

public class ChangeColorHandlerTests
{
    [Fact]
    public async Task FreeColor_ChangesCallersColor()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        await ChangeColor(context, other.Id, PlayerColors.Palette[2]);

        Assert.Equal(PlayerColors.Palette[2], await ColorOf(context, other.Id));
    }

    [Fact]
    public async Task ColorTakenByOther_ThrowsColorIsAlreadyTaken()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();
        var colorBefore = await ColorOf(context, other.Id);

        await Assert.ThrowsAsync<ColorIsAlreadyTakenException>(() => ChangeColor(context, other.Id, PlayerColors.Palette[0]));

        Assert.Equal(colorBefore, await ColorOf(context, other.Id));
    }

    [Fact]
    public async Task DuringRace_ThrowsLobbyInRace()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);

        await Assert.ThrowsAsync<LobbyInRaceException>(() => ChangeColor(context, other.Id, PlayerColors.Palette[2]));
    }

    [Fact]
    public async Task CallerNotInLobby_ThrowsPlayerNotInLobby()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<PlayerNotInLobbyException>(() => ChangeColor(context, Guid.NewGuid(), PlayerColors.Palette[2]));
    }

    private static Task ChangeColor(LobbyTestContext context, Guid callerId, string color) =>
        new ChangeColorHandler(context.Lobbies)
            .Handle(new ChangeColorCommand(callerId, color), CancellationToken.None);

    private static Task<string> ColorOf(LobbyTestContext context, Guid playerId) =>
        context.ReadLobby(l => l.Players.Single(p => p.Id == playerId).Color);
}
