namespace FasterNFaster.Api.Core.Interfaces.Events;

public interface IRaceEvent
{
    void WrapRaceContext(Guid lobbyId);
}