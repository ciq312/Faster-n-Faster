using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Factories.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public class JoinLobbyHandler(IUserFactory userFactory, ILobbyService lobbyService) : IRequestHandler<JoinLobbyCommand>
{
    private readonly IUserFactory userFactory = userFactory;
    private readonly ILobbyService lobbyService = lobbyService;

    public async Task Handle(JoinLobbyCommand command, CancellationToken cancellationToken)
    {
        User user = await userFactory.GetUser(command.PlayerId, command.Nick, command.Role);
        await lobbyService.JoinLobby(user, command.LobbyId, command.InviteCode);
    }
}
