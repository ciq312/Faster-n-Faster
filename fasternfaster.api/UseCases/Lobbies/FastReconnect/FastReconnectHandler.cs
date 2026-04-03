using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.FastReconnect;

public class FastReconnectHandler : IHandler<FastReconnectCommand>
{
    private readonly ILobbyService _lobbyService;

    private readonly int RECONNECT_GRACE_PERIOD_SECONDS = 15;

    public FastReconnectHandler(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task Handle(FastReconnectCommand command)
    {
        var cts = new CancellationTokenSource();
        _lobbyService.StorePendingRemoval(command.LobbyId, command.PlayerId, cts);

        await Task.Delay(RECONNECT_GRACE_PERIOD_SECONDS * 1000, cts.Token);
    }
}



public record FastReconnectCommand(Guid LobbyId, Guid PlayerId);
