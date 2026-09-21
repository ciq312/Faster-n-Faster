using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.RaceFinished;

public class BroadcastRaceFinishedHandler(IBroadcaster broadcaster)
    : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public async Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        var results = e.Results
            .Select(r => new RaceResultDTO(r.LobbyPlayerId, r.Nick, r.FinishPosition, r.WPM, r.Accuracy, r.MistakeCount))
            .ToList();

        await broadcaster.Broadcast(Audience.Lobby(e.LobbyId), GameEvents.RaceEnded, new RaceEndedDTO(results));
    }
}
