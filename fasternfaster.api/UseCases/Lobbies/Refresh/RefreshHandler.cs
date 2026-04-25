using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;
using FasterNFaster.Api.UseCases.Services;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public class RefreshHandler(ILobbyService lobbyService) : IHandler<RefreshCommand>
{
    private readonly ILobbyService lobbyService = lobbyService;
    public Task Handle(RefreshCommand command)
    {
        if (!lobbyService.TryGetPendingRemoval(command.LobbyId, command.UserId, out var cts))
            cts.Cancel();

        return Task.CompletedTask;
    }
}
