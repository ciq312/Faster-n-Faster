using FasterNFaster.Api.UseCases.Lobbies.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using Shouldly;

namespace FasterNFaster.UnitTests.Lobbies;

public class GetLobbiesHandlerTests
{
    [Fact]
    public async Task NoCursor_ReturnsNewestFirst()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var created = await CreateLobbies(context, 3);

        var result = await List(context, limit: 20);

        result.Items.Select(i => i.Id).ShouldBe([created[2], created[1], created[0], context.LobbyId]);
        result.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task PrivateLobby_IsNotListed()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await CreateLobbies(context, 1, isPrivate: true);

        var result = await List(context, limit: 20);

        result.Items.Select(i => i.Id).ShouldBe([context.LobbyId]);
    }

    [Fact]
    public async Task PrivateLobbies_DoNotShortenThePage()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await CreateLobbies(context, 3, isPrivate: true);
        var publicLobbies = await CreateLobbies(context, 2);

        var result = await List(context, limit: 2);

        result.Items.Select(i => i.Id).ShouldBe([publicLobbies[1], publicLobbies[0]]);
        result.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task MoreLobbiesThanLimit_ReturnsNextCursor()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await CreateLobbies(context, 2);

        var result = await List(context, limit: 2);

        result.Items.Count.ShouldBe(2);
        result.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExactlyLimitLobbies_ReturnsNullNextCursor()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await CreateLobbies(context, 2);

        var result = await List(context, limit: 3);

        result.Items.Count.ShouldBe(3);
        result.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task WalkingEveryPage_ReturnsEachLobbyOnce()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var created = await CreateLobbies(context, 6);

        var walked = await WalkEveryPage(context, limit: 3);

        walked.ShouldBe([.. created.AsEnumerable().Reverse(), context.LobbyId]);
    }

    [Fact]
    public async Task SameCreatedAt_PagesByIdWithoutGapsOrDuplicates()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var created = await CreateLobbies(context, 4, advanceClock: false);

        var walked = await WalkEveryPage(context, limit: 2);

        walked.ShouldBe([.. created.Append(context.LobbyId).Order()]);
    }

    [Fact]
    public async Task LobbyCreatedBetweenPages_DoesNotShiftLaterPages()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var created = await CreateLobbies(context, 3);
        var firstPage = await List(context, limit: 2);

        await CreateLobbies(context, 1);
        var secondPage = await List(context, limit: 2, firstPage.NextCursor);

        secondPage.Items.Select(i => i.Id).ShouldBe([created[0], context.LobbyId]);
    }

    [Fact]
    public async Task LobbyRemovedBetweenPages_NextPageContinuesAfterIt()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var created = await CreateLobbies(context, 3);
        var firstPage = await List(context, limit: 2);
        var cursorLobby = firstPage.Items[^1].Id;
        (await context.Lobbies.RemoveIfEmpty(cursorLobby)).ShouldBeTrue();
        context.Races.Remove(cursorLobby);

        var secondPage = await List(context, limit: 2, firstPage.NextCursor);

        secondPage.Items.Select(i => i.Id).ShouldBe([created[0], context.LobbyId]);
    }

    [Fact]
    public async Task LobbyWithoutRace_IsSkipped()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await context.Lobbies.Create("No race", false, Guid.NewGuid());

        var result = await List(context, limit: 20);

        result.Items.Select(i => i.Id).ShouldBe([context.LobbyId]);
    }

    [Fact]
    public async Task LimitAboveMax_ClampedTo100()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await CreateLobbies(context, 100);

        var result = await List(context, limit: 500);

        result.Items.Count.ShouldBe(100);
        result.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task LimitBelowOne_ReturnsOneLobby()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        await CreateLobbies(context, 2);

        var result = await List(context, limit: 0);

        result.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task MalformedCursor_ThrowsInvalidLobbyCursor()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());

        await Should.ThrowAsync<InvalidLobbyCursorException>(() => List(context, limit: 20, cursor: "%%%"));
    }

    [Fact]
    public async Task NoActiveRace_ReportsSessionInactive()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var result = await List(context, limit: 20);

        var item = result.Items.ShouldHaveSingleItem();
        item.IsSessionActive.ShouldBeFalse();
        item.PlayerCount.ShouldBe(2);
    }

    [Fact]
    public async Task RaceStarted_ReportsSessionActive()
    {
        var (host, _, context) = await LobbyFactory.TwoUsersSetup();
        await LobbyFactory.StartRace(context, host.Id);

        var result = await List(context, limit: 20);

        result.Items.ShouldHaveSingleItem().IsSessionActive.ShouldBeTrue();
    }

    private static Task<GetLobbiesResult> List(LobbyTestContext context, int limit, string? cursor = null) =>
        new GetLobbiesHandler(context.Lobbies, context.Races)
            .Handle(new GetLobbiesQuery(limit, cursor), CancellationToken.None);

    private static async Task<List<Guid>> WalkEveryPage(LobbyTestContext context, int limit)
    {
        var walked = new List<Guid>();
        string? cursor = null;
        do
        {
            var page = await List(context, limit, cursor);
            walked.AddRange(page.Items.Select(i => i.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        return walked;
    }

    private static async Task<List<Guid>> CreateLobbies(LobbyTestContext context, int count, bool isPrivate = false, bool advanceClock = true)
    {
        var created = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            if (advanceClock) context.Time.Advance(TimeSpan.FromSeconds(1));
            var lobby = await LobbyFactory.CreateLobby(context, $"Lobby {i}", isPrivate);
            created.Add(lobby.LobbyId);
        }

        return created;
    }
}
