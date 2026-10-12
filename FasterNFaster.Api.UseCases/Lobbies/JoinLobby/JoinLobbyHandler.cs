using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public class JoinLobbyHandler(ILobbyStore lobbies, ILobbyStateTracker tracker) : IRequestHandler<JoinLobbyCommand>
{
    public async Task Handle(JoinLobbyCommand command, CancellationToken cancellationToken)
    {
        await lobbies.Mutate(command.LobbyId, l => l.Join(
            command.PlayerId,
            command.Nick,
            command.InviteCode,
            id => lobbies.TryClaimPlayer(id, command.LobbyId)));
        tracker.MarkChanged(command.LobbyId);
    }
}
