using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Races.Events
{
    public record RaceFinishedEvent(Guid LobbyId, IEnumerable<RaceParticipantResult> Results) : IDomainEvent
    {
        public Task Dispatch(IEventDispatcher eventDispatcher) => eventDispatcher.Dispatch(this);
    }
}