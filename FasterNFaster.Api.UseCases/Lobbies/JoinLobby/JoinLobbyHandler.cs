using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public class JoinLobbyHandler(ILobbyAccess lobbies) : IRequestHandler<JoinLobbyCommand>
{
    public async Task Handle(JoinLobbyCommand command, CancellationToken cancellationToken)
    {
        if (lobbies.GetLobbyIdOfPlayer(command.PlayerId) is Guid existingId && existingId != command.LobbyId)
            throw new AlreadyInLobbyException();

        await lobbies.Mutate(command.LobbyId, l => l.Join(command.PlayerId, command.Nick, command.InviteCode));
    }
}
