using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Races.RefreshPassage;
using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.RaceEndOrchestration;

public class RaceFinishedOrchestrationHandler(
    ILobbyStore lobbies,
    PassageRefresher passageRefresher,
    IRaceTickRegistry registry,
    ILobbyStateScope lobbyStateScope) : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken) =>
        lobbyStateScope.Run(async () =>
        {
            var e = notification.Event;

            registry.DeregisterLobby(e.LobbyId);

            await lobbies.Mutate(e.LobbyId, l => l.EndSession());

            var passage = await passageRefresher.FetchNext(e.LobbyId);

            if (await lobbies.TryRead(e.LobbyId, l => !l.IsSessionActive))
                await passageRefresher.TryApply(e.LobbyId, passage);
        });
}
