using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public class DisconnectHandler(ILobbyAccess lobbies, IRaceAccess races) : IRequestHandler<DisconnectCommand>
{
    public async Task Handle(DisconnectCommand command, CancellationToken cancellationToken)
    {
        var lobbyId = lobbies.GetLobbyIdOfPlayer(command.PlayerId);
        if (lobbyId is null) return;

        var wasRacing = false;

        await lobbies.Mutate(lobbyId.Value, l =>
        {
            wasRacing = l.IsSessionActive;
            l.Disconnect(command.PlayerId);
        });

        if (wasRacing)
            await races.TryMutate(lobbyId.Value, r => r.WithdrawParticipant(command.PlayerId));
    }
}
