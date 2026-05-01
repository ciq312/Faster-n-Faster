using System.ComponentModel;
using FastEndpoints;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(ILobbyService lobbyService) : IDomainEventHandler<PlayerRemovedEvent>
{
    private readonly ILobbyService lobbyService = lobbyService;

    public async Task Handle(PlayerRemovedEvent domainEvent)
    {
        await lobbyService.RemoveLobbyIfEmpty(domainEvent.LobbyId);
    }

}