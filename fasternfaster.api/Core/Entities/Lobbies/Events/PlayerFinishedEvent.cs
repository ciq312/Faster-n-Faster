using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public record PlayerFinishedEvent(
    string Nick,
    Guid PlayerId,
    int? FinishPosition,
    double Wpm,
    double Accuracy) : IDomainEvent, IRaceEvent
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

    public Task Dispatch(IEventDispatcher dispatcher, CancellationToken ct = default) =>
        dispatcher.Dispatch(this, ct);
}
