using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public class DisconnectHandler(ILobbyStore lobbies, IRaceStore races, ILobbyStateTracker tracker) : IRequestHandler<DisconnectCommand>
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
        tracker.MarkChanged(lobbyId.Value);

        if (wasRacing)
            await races.TryMutate(lobbyId.Value, r => r.WithdrawParticipant(command.PlayerId));
    }
}
