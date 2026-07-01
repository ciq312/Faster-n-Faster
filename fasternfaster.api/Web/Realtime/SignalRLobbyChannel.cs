using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.Realtime;

public class SignalRLobbyChannel(IHubContext<GameHub> hub, ISessionService sessionService) : ILobbyChannel
{
    public Task Join(Guid userId, Guid lobbyId) =>
        WithConnection(userId, connectionId => hub.Groups.AddToGroupAsync(connectionId, LobbyGroup(lobbyId)));

    public Task Leave(Guid userId, Guid lobbyId) =>
        WithConnection(userId, connectionId => hub.Groups.RemoveFromGroupAsync(connectionId, LobbyGroup(lobbyId)));

    private Task WithConnection(Guid userId, Func<string, Task> action)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        return connectionId is null ? Task.CompletedTask : action(connectionId);
    }
}
