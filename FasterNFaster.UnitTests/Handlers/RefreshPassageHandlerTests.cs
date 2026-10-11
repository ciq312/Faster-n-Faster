using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

namespace FasterNFaster.UnitTests.Handlers;

public class RefreshPassageHandlerTests
{
    [Fact]
    public async Task ByNonHost_ThrowsNotHost()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        await Assert.ThrowsAsync<NotHostException>(() => Refresh(context, other.Id));
    }

    [Fact]
    public async Task DuringRace_ThrowsLobbyInRace()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);

        await Assert.ThrowsAsync<LobbyInRaceException>(() => Refresh(context, host.Id));
    }

    [Fact]
    public async Task ByHost_DoesNotThrow()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();

        var exception = await Record.ExceptionAsync(() => Refresh(context, host.Id));

        Assert.Null(exception);
    }

    private static Task Refresh(LobbyTestContext context, Guid callerId) =>
        new RefreshPassageHandler(context.LobbyAccess, context.RaceAccess)
            .Handle(new RefreshPassageCommand(callerId), CancellationToken.None);
}
