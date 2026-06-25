using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(ILobbySessionService lobbySessionService) : INotificationHandler<PlayerRemovedEvent>
{
    private readonly ILobbySessionService lobbySessionService = lobbySessionService;

    public async Task Handle(PlayerRemovedEvent domainEvent, CancellationToken cancellationToken)
    {
        await lobbySessionService.RemoveLobbyIfEmpty(domainEvent.LobbyId);
    }

}