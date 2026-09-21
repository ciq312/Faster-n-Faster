using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public class DisconnectHandler(ILobbyAccess lobbies, IRaceAccess races) : IRequestHandler<DisconnectCommand>
{
    public async Task Handle(DisconnectCommand command, CancellationToken cancellationToken)
    {
        Lobby lobby = lobbies.GetOfPlayerRequired(command.PlayerId);

        await lobbies.Mutate(lobby.Id, l => l.Disconnect(command.PlayerId));

        if (lobby.IsSessionActive)
            await races.Mutate(lobby.Id, r => r.WithdrawParticipant(command.PlayerId));
    }
}
