using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public record PlayerFinishedEvent(
    string Nick,
    Guid LobbyId,
    Guid PlayerId,
    int? FinishPosition,
    double Wpm,
    double Accuracy) : IDomainEvent
{
    public Task Dispatch(IEventDispatcher eventDispatcher) => eventDispatcher.Dispatch(this);

}
