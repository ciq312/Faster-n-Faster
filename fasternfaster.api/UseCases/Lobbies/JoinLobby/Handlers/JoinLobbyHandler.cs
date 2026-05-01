using FastEndpoints;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Factories.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Handlers;

public class JoinLobbyHandler(IUserFactory userFactory, ILobbyService lobbyService) : IHandler<JoinLobbyCommand>
{
    private readonly IUserFactory userFactory = userFactory;
    private readonly ILobbyService lobbyService = lobbyService;

    public async Task Handle(JoinLobbyCommand command)
    {
        User user = await userFactory.GetUser(command.PlayerId, command.Nick, command.Role);
        await lobbyService.JoinLobby(user, command.LobbyId, command.InviteCode);
    }

    private bool IsCodeCorrect(string? codeToCheck, string? actualCode)
    {
        return string.Equals(codeToCheck, actualCode, StringComparison.OrdinalIgnoreCase);
    }
}
