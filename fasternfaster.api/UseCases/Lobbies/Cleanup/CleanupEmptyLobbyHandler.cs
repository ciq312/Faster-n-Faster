using System.ComponentModel;
using FastEndpoints;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.Core.Lobbies.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.UseCases.Lobbies.Cleanup;

public class CleanupEmptyLobbyHandler(ILobbySessionService lobbySessionService) : IDomainEventHandler<PlayerRemovedEvent>
{
    private readonly ILobbySessionService lobbySessionService = lobbySessionService;

    public async Task Handle(PlayerRemovedEvent domainEvent)
    {
        await lobbySessionService.RemoveLobbyIfEmpty(domainEvent.LobbyId);
    }

}