using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbyByInviteCode;
using Shouldly;

namespace FasterNFaster.UnitTests.Lobbies;

public class GetLobbyByInviteCodeHandlerTests
{
    [Fact]
    public async Task MatchingCode_ReturnsThatLobby()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();
        var code = await context.ReadLobby(l => l.LobbySettings.InviteCode);

        var item = await Find(context, code);

        item.Id.ShouldBe(context.LobbyId);
        item.PlayerCount.ShouldBe(2);
    }

    [Fact]
    public async Task PrivateLobby_FoundByCode()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var created = await LobbyFactory.CreateLobby(context, "Private", isPrivate: true);

        var item = await Find(context, created.InviteCode.ShouldNotBeNull());

        item.Id.ShouldBe(created.LobbyId);
        item.IsPrivate.ShouldBeTrue();
    }

    [Fact]
    public async Task CodeInOtherCase_ReturnsThatLobby()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var code = await context.ReadLobby(l => l.LobbySettings.InviteCode);

        var item = await Find(context, code.ToLowerInvariant());

        item.Id.ShouldBe(context.LobbyId);
    }

    [Fact]
    public async Task UnknownCode_ThrowsInvalidInviteCode()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        await Should.ThrowAsync<InvalidInviteCodeException>(() => Find(context, "NOPE"));
    }

    [Fact]
    public async Task LobbyWithoutRace_ThrowsInvalidInviteCode()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var (_, code) = await context.Lobbies.Create("No race", false, Guid.NewGuid());

        await Should.ThrowAsync<InvalidInviteCodeException>(() => Find(context, code));
    }

    private static Task<LobbyListItem> Find(LobbyTestContext context, string code) =>
        new GetLobbyByInviteCodeHandler(context.Lobbies, context.Races)
            .Handle(new GetLobbyByInviteCodeQuery(code), CancellationToken.None);
}
