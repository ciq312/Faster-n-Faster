using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.UnitTests.Realtime;

public class LobbyStateContractTests
{
    [Fact]
    public async Task LobbyState_SerializesPassageAndWordCountUnderSettings()
    {
        var (_, _, context) = await LobbyFactory.TwoUsersSetup();

        var json = await SerializedLobbyState(context);

        var settings = json["settings"]!.AsObject();
        Assert.False(string.IsNullOrEmpty(settings["passage"]!.GetValue<string>()));
        Assert.True(settings["wordCount"]!.GetValue<int>() > 0);
    }

    [Fact]
    public async Task LobbyState_SerializesLobbyAndPlayerFieldsTheClientReads()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var json = await SerializedLobbyState(context);

        Assert.Equal(context.LobbyId, json["lobbyId"]!.GetValue<Guid>());
        Assert.Equal("Test", json["lobbyName"]!.GetValue<string>());
        Assert.False(json["isSessionActive"]!.GetValue<bool>());
        Assert.False(json["isPrivate"]!.GetValue<bool>());
        Assert.False(string.IsNullOrEmpty(json["inviteCode"]!.GetValue<string>()));
        Assert.True(json["maxPlayers"]!.GetValue<int>() > 0);

        var colors = json["colors"]!.AsArray();
        Assert.NotEmpty(colors);
        Assert.All(colors, c =>
        {
            Assert.False(string.IsNullOrEmpty(c!["color"]!.GetValue<string>()));
            Assert.NotNull(c["isAvailable"]);
        });

        var players = json["players"]!.AsArray();
        Assert.Equal(2, players.Count);
        var hostJson = Assert.Single(players, p => p!["id"]!.GetValue<Guid>() == host.Id)!;
        var otherJson = Assert.Single(players, p => p!["id"]!.GetValue<Guid>() == other.Id)!;
        Assert.True(hostJson["isHost"]!.GetValue<bool>());
        Assert.False(otherJson["isHost"]!.GetValue<bool>());
        Assert.Equal(host.Nick, hostJson["nick"]!.GetValue<string>());
        Assert.False(string.IsNullOrEmpty(hostJson["color"]!.GetValue<string>()));
    }

    private static async Task<JsonObject> SerializedLobbyState(LobbyTestContext context)
    {
        var state = await context.LobbyQuery.GetLobbyState(context.LobbyId);
        var options = new JsonHubProtocolOptions().PayloadSerializerOptions;

        return JsonSerializer.SerializeToNode(state, state.GetType(), options)!.AsObject();
    }
}
