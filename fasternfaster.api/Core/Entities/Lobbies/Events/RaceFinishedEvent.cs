using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public record RaceFinishedPlayerData(string Nick, Guid PlayerId, int? FinishPosition, double Wpm, double Accuracy, int Mistakes);

public record RaceFinishedEvent(
    Guid LobbyId,
    IReadOnlyList<RaceFinishedPlayerData> Results) : IDomainEvent
{
    public Task Dispatch(IEventDispatcher eventDispatcher) => eventDispatcher.Dispatch(this);

}
