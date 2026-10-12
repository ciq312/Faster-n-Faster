using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Races;

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

    [Fact]
    public async Task RaceStartsWhileFetchingPassage_ThrowsLobbyInRaceAndKeepsPassage()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        var passageBefore = await context.ReadRace(r => r.Passage);
        var provider = new GatedPassageProvider();
        var handler = new RefreshPassageHandler(context.Lobbies, new PassageRefresher(context.Lobbies, context.Races, provider, context.Tracker));

        var refresh = handler.Handle(new RefreshPassageCommand(host.Id), CancellationToken.None);
        await provider.WaitForRequest();
        await LobbyFactory.StartRace(context, host.Id);
        provider.Complete("fresh words");

        await Assert.ThrowsAsync<LobbyInRaceException>(() => refresh);
        Assert.Equal(passageBefore, await context.ReadRace(r => r.Passage));
    }

    private static Task Refresh(LobbyTestContext context, Guid callerId) =>
        new RefreshPassageHandler(context.Lobbies, context.PassageRefresher)
            .Handle(new RefreshPassageCommand(callerId), CancellationToken.None);
}
