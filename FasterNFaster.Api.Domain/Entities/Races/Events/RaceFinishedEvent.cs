using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Races.Events;

public record RaceFinishedEvent(IEnumerable<RaceParticipantResult> Results) : IDomainEvent, IRaceEvent
{
    public Guid LobbyId
    {
        get
        {
            if (field == default) throw new NullReferenceException("LobbyId isn't wrapped");
            return field;
        }
        private set;
    }

    public void WrapRaceContext(Guid lobbyId) => LobbyId = lobbyId;
}
