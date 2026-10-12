using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

namespace FasterNFaster.UnitTests.Lobbies;

public class GetLobbiesHandlerTests
{
    [Fact]
    public async Task NoInviteCode_ReturnsLobbyWithPlayerCount()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        var item = Assert.Single(result.Lobbies);
        Assert.Equal(context.LobbyId, item.Id);
        Assert.Equal(2, item.PlayerCount);
    }

    [Fact]
    public async Task MatchingInviteCode_ReturnsThatLobby()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var code = await context.ReadLobby(l => l.LobbySettings.InviteCode);

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
        await context.Lobbies.Create("No race", false, Guid.NewGuid());

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        Assert.Equal(context.LobbyId, Assert.Single(result.Lobbies).Id);
    }

    [Fact]
    public async Task SeveralLobbies_ReturnedNewestFirst()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var createLobbyHandler = new CreateLobbyHandler(new RandomPassageProvider(), context.Lobbies, context.Races);
        for (var i = 0; i < 3; i++)
            await createLobbyHandler.Handle(new CreateLobbyCommand($"Lobby {i}", false, Guid.NewGuid()), CancellationToken.None);

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        Assert.Equal(4, result.Lobbies.Count);
        Assert.Equal(result.Lobbies.OrderByDescending(l => l.CreatedAt), result.Lobbies);
    }

    [Fact]
    public async Task NoActiveRace_ReportsSessionInactive()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        Assert.False(Assert.Single(result.Lobbies).IsSessionActive);
    }

    [Fact]
    public async Task RaceStarted_ReportsSessionActive()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);

        var result = await Handler(context).Handle(new GetLobbiesQuery(), CancellationToken.None);

        Assert.True(Assert.Single(result.Lobbies).IsSessionActive);
    }

    private static GetLobbiesHandler Handler(LobbyTestContext context) =>
        new(context.Lobbies, context.Races);
}
