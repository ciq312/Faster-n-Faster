using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.FastReconnect;

public class FastReconnectHandler(ILobbyService lobbyService) : IHandler<FastReconnectCommand>
{
    private readonly ILobbyService lobbyService = lobbyService;

    private readonly int RECONNECT_GRACE_PERIOD_SECONDS = 15;

    public async Task Handle(FastReconnectCommand command)
    {
        var cts = new CancellationTokenSource();
        lobbyService.StorePendingRemoval(command.LobbyId, command.PlayerId, cts);

        await Task.Delay(RECONNECT_GRACE_PERIOD_SECONDS * 1000, cts.Token);
    }
}



public record FastReconnectCommand(Guid LobbyId, Guid PlayerId);
