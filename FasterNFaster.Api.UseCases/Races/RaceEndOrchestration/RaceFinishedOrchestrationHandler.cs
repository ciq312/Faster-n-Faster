using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Races.Services;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.RaceEndOrchestration;

public class RaceFinishedOrchestrationHandler(
    ILobbyAccess lobbies,
    IRaceAccess races,
    IRaceTickRegistry registry,
    ILobbyStateScope lobbyStateScope) : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken) =>
        lobbyStateScope.Run(async () =>
        {
            var e = notification.Event;

            await lobbies.Mutate(e.LobbyId, l => l.EndSession());

            await races.RefreshPassage(e.LobbyId);

            registry.DeregisterLobby(e.LobbyId);
        });
}
