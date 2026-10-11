using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

namespace FasterNFaster.UnitTests.Handlers;

public class GetLobbiesHandlerTests
{
    [Fact]
    public async Task NoInviteCode_ReturnsLobbyWithPlayerCountAndGameMode()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        var item = Assert.Single(result.Lobbies);
        Assert.Equal(context.LobbyId, item.Id);
        Assert.Equal(2, item.PlayerCount);
        Assert.Equal((await context.RaceAccess.GetRaceSettings(context.LobbyId)).RaceType, item.GameMode);
    }

    [Fact]
    public async Task MatchingInviteCode_ReturnsThatLobby()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var code = context.Lobby.LobbySettings.InviteCode;

        var result = await Handler(context).Handle(new GetLobbiesQuery(code), CancellationToken.None);

        Assert.Equal(context.LobbyId, Assert.Single(result.Lobbies).Id);
    }

    [Fact]
    public async Task UnknownInviteCode_ReturnsEmpty()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var result = await Handler(context).Handle(new GetLobbiesQuery("NOPE"), CancellationToken.None);

        Assert.Empty(result.Lobbies);
    }

    [Fact]
    public async Task LobbyWithoutRace_IsSkipped()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await context.LobbyAccess.Create("No race", false, Guid.NewGuid());

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        Assert.Equal(context.LobbyId, Assert.Single(result.Lobbies).Id);
    }

    [Fact]
    public async Task SeveralLobbies_ReturnedNewestFirst()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var createLobbyHandler = new CreateLobbyHandler(new RandomPassageProvider(), context.LobbyAccess, context.RaceAccess);
        for (var i = 0; i < 3; i++)
            await createLobbyHandler.Handle(new CreateLobbyCommand($"Lobby {i}", false, Guid.NewGuid()), CancellationToken.None);

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        Assert.Equal(4, result.Lobbies.Count);
        Assert.Equal(result.Lobbies.OrderByDescending(l => l.CreatedAt), result.Lobbies);
    }

    private static GetLobbiesHandler Handler(LobbyTestContext context) =>
        new(context.LobbyAccess, context.RaceAccess);
}
