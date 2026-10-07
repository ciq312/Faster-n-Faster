using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.AntiCheat;

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
