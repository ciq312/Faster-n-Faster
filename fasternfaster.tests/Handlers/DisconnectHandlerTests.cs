using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using Microsoft.AspNetCore.Identity.Data;
using MimeKit;

namespace FasterNFaster.Tests.Handlers;

public class DisconnectHandlerTests
{
    [Fact]
    public async Task DisconnectFromLobby_ShouldRemove()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyService);

        await disconnectHandler.Handle(new DisconnectCommand(other.Id));

        Assert.Single(context.Lobby.Players);
        Assert.True(context.Lobby.Players.ToList()[0].User.Id == host.Id);
    }
    [Fact]
    public async Task HostDisconnectFromLobby_ShouldPromoteNextAndRemoveHost()
    {
        var (host, other, context) = await LobbyFactory.TwoUsersSetup();

        var disconnectHandler = new DisconnectHandler(context.LobbyService);

        await disconnectHandler.Handle(new DisconnectCommand(host.Id));

        Assert.Single(context.Lobby.Players);
        Assert.True(context.Lobby.Players.ToList()[0].User.Id == context.Lobby.HostId);
    }
    private void PrintCollection<T>(ICollection<T> values)
    {
        foreach (var value in values) Console.WriteLine(value);
    }

}