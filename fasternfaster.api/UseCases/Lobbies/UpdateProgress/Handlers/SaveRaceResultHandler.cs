using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class SaveRaceResultHandler(IUserProfileService profileService)
    : INotificationHandler<DomainEventNotification<RaceFinishedEvent>>
{
    public async Task Handle(DomainEventNotification<RaceFinishedEvent> notification, CancellationToken cancellationToken)
    {
        await profileService.ProcessRaceResultsAsync(notification.Event.Results);
    }
}
