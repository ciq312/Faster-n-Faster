using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

namespace FasterNFaster.UnitTests.Handlers;

public class JoinLobbyHandlerTests
{
    [Fact]
    public async Task OpenLobby_AddsAndTracksPlayer()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var joiner = new User("joiner");

        await Join(context, joiner.Id, context.LobbyId);

        Assert.True(context.Lobby.IsPlayerIn(joiner.Id));
        Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(joiner.Id));
    }

    [Fact]
    public async Task PlayerInAnotherLobby_ThrowsAlreadyInLobby()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();
        var anotherLobby = await context.LobbyAccess.Create("Other", false, Guid.NewGuid());

        await Assert.ThrowsAsync<AlreadyInLobbyException>(() => Join(context, other.Id, anotherLobby.Id));

        Assert.Equal(context.LobbyId, context.LobbyAccess.GetLobbyIdOfPlayer(other.Id));
    }

    [Fact]
    public async Task SameLobbyAgain_IsNoOp()
    {
        var (_, other, context) = await LobbyFactory.TwoUsersSetup();

        var exception = await Record.ExceptionAsync(() => Join(context, other.Id, context.LobbyId));

        Assert.Null(exception);
        Assert.Equal(2, context.Lobby.Players.Count);
    }

    [Fact]
    public async Task UnknownLobby_ThrowsLobbyNotFound()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var joiner = new User("joiner");

        await Assert.ThrowsAsync<LobbyNotFoundException>(() => Join(context, joiner.Id, Guid.NewGuid()));

        Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(joiner.Id));
    }

    [Fact]
    public async Task DuringRace_ThrowsLobbyIsNotAcceptingPlayers_AndDoesNotTrack()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);
        var joiner = new User("joiner");

        await Assert.ThrowsAsync<LobbyIsNotAcceptingPlayersException>(() => Join(context, joiner.Id, context.LobbyId));

        Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(joiner.Id));
    }

    [Fact]
    public async Task PrivateLobbyWithWrongCode_ThrowsInvalidInviteCode_AndDoesNotTrack()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var privateLobby = await context.LobbyAccess.Create("Private", true, Guid.NewGuid());
        var joiner = new User("joiner");

        await Assert.ThrowsAsync<InvalidInviteCodeException>(() => Join(context, joiner.Id, privateLobby.Id, "wrong"));

        Assert.Null(context.LobbyAccess.GetLobbyIdOfPlayer(joiner.Id));
    }

    private static Task Join(LobbyTestContext context, Guid playerId, Guid lobbyId, string? inviteCode = null) =>
        new JoinLobbyHandler(context.LobbyAccess)
            .Handle(new JoinLobbyCommand(playerId, lobbyId, "joiner", inviteCode), CancellationToken.None);
}
