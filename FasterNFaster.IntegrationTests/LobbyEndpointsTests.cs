using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using Shouldly;

namespace FasterNFaster.IntegrationTests;

public class LobbyEndpointsTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task ByInvite_PrivateLobby_ReturnsIt()
    {
        var client = await GuestClientAsync();
        var created = await LobbyHelper.CreateLobbyAsync(client, isPrivate: true);

        var lobby = await client.GetFromJsonAsync<LobbyListItem>($"api/lobbies/by-invite/{created.InviteCode}");

        var item = lobby.ShouldNotBeNull();
        item.Id.ShouldBe(created.LobbyId);
        item.IsPrivate.ShouldBeTrue();
    }

    [Fact]
    public async Task ByInvite_UnknownCode_Returns404InvalidInviteCode()
    {
        var response = await App.CreateClient().GetAsync("api/lobbies/by-invite/NOPE");

        await ShouldBeErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.InvalidInviteCode);
    }

    [Fact]
    public async Task List_WalkingEveryPage_ReturnsEachPublicLobbyOnce()
    {
        var client = await GuestClientAsync();
        var publicLobbies = new List<Guid>();
        for (var i = 0; i < 5; i++)
            publicLobbies.Add((await LobbyHelper.CreateLobbyAsync(client, isPrivate: false)).LobbyId);
        await LobbyHelper.CreateLobbyAsync(client, isPrivate: true);
        await LobbyHelper.CreateLobbyAsync(client, isPrivate: true);

        var walked = await WalkEveryPageAsync(client, limit: 2);

        walked.Order().ShouldBe(publicLobbies.Order());
        walked.ShouldBeUnique();
    }

    [Fact]
    public async Task List_NeverReturnsPrivateLobbies()
    {
        var client = await GuestClientAsync();
        var publicLobby = await LobbyHelper.CreateLobbyAsync(client, isPrivate: false);
        await LobbyHelper.CreateLobbyAsync(client, isPrivate: true);

        var page = await client.GetFromJsonAsync<GetLobbiesResult>("api/lobbies");

        page.ShouldNotBeNull().Items.Select(i => i.Id).ShouldBe([publicLobby.LobbyId]);
    }

    [Fact]
    public async Task List_WithoutLimit_ReturnsFirst20AndCursor()
    {
        var client = await GuestClientAsync();
        for (var i = 0; i < 21; i++)
            await LobbyHelper.CreateLobbyAsync(client, isPrivate: false);

        var page = await client.GetFromJsonAsync<GetLobbiesResult>("api/lobbies");

        var result = page.ShouldNotBeNull();
        result.Items.Count.ShouldBe(20);
        result.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task List_MalformedCursor_Returns400InvalidCursor()
    {
        var response = await App.CreateClient().GetAsync("api/lobbies?cursor=%25%25%25");

        await ShouldBeErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.InvalidCursor);
    }

    private async Task<HttpClient> GuestClientAsync()
    {
        var client = App.CreateClient();
        await AuthHelper.RegisterGuestAsync(client);
        return client;
    }

    private static async Task<List<Guid>> WalkEveryPageAsync(HttpClient client, int limit)
    {
        var walked = new List<Guid>();
        string? cursor = null;
        do
        {
            var uri = cursor is null ? $"api/lobbies?limit={limit}" : $"api/lobbies?limit={limit}&cursor={cursor}";
            var page = await client.GetFromJsonAsync<GetLobbiesResult>(uri);
            page.ShouldNotBeNull();
            walked.AddRange(page.Items.Select(i => i.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        return walked;
    }
}
