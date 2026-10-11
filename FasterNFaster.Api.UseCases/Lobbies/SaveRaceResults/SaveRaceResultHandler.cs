using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.SaveRaceResults;

public class SaveRaceResultHandler(IRaceResultQueue queue)
    : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken)
    {
        queue.Enqueue(notification.Event.Results);
        return Task.CompletedTask;
    }
}
