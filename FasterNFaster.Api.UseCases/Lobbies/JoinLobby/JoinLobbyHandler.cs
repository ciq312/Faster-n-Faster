using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Factories.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public class JoinLobbyHandler(IUserFactory userFactory, ILobbyAccess lobbies) : IRequestHandler<JoinLobbyCommand>
{
    public async Task Handle(JoinLobbyCommand command, CancellationToken cancellationToken)
    {
        User user = await userFactory.GetUser(command.PlayerId, command.Nick, command.Role);

        if (lobbies.GetLobbyIdOfPlayer(user.Id) is Guid existingId && existingId != command.LobbyId)
            throw new AlreadyInLobbyException();

        await lobbies.Mutate(command.LobbyId, l => l.Join(user.Id, user.Nick, command.InviteCode));
    }
}
