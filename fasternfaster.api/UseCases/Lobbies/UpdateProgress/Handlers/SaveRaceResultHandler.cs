using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress.Handlers;

public class SaveRaceResultHandler(IUserProfileService profileService) : INotificationHandler<RaceFinishedEvent>
{
    public async Task Handle(RaceFinishedEvent domainEvent, CancellationToken cancellationToken)
    {
        await profileService.ProcessRaceResultsAsync(domainEvent.Results);
    }
}