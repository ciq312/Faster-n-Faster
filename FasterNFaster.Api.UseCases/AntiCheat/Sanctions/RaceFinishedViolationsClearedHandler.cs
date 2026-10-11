using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.AntiCheat.Sanctions;

// Race.Id is reused across race runs in the same lobby (see Race.Reset()), so the per-race
// violation count is keyed by LobbyId and must be cleared here rather than relying on Race.Id.
public class RaceFinishedViolationsClearedHandler(IStrikeRegistry strikes)
    : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken)
    {
        strikes.ClearRace(notification.Event.LobbyId);
        return Task.CompletedTask;
    }
}
