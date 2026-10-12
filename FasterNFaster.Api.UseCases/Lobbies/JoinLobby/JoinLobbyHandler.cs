using FasterNFaster.Api.UseCases.Lobbies.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public class JoinLobbyHandler(ILobbyStore lobbies) : IRequestHandler<JoinLobbyCommand>
{
    public Task Handle(JoinLobbyCommand command, CancellationToken cancellationToken) =>
        lobbies.Mutate(command.LobbyId, l => l.Join(
            command.PlayerId,
            command.Nick,
            command.InviteCode,
            id => lobbies.TryClaimPlayer(id, command.LobbyId)));
}
