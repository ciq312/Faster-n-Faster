using System.Net.Http.Json;
using FasterNFaster.Api.Presentation.Web.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using Shouldly;

namespace FasterNFaster.IntegrationTests;

public static class LobbyHelper
{
    public static async Task<CreateLobbyResult> CreateLobbyAsync(HttpClient client, bool isPrivate = false)
    {
        var response = await client.PostAsJsonAsync("api/lobbies", new CreateLobbyRequest("test", isPrivate));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateLobbyResult>();
        return result.ShouldNotBeNull();
    }
}
