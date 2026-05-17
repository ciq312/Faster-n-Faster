namespace FasterNFaster.Api.Core.Interfaces.Events;

internal interface IRaceEvent
{
    void WrapRaceContext(Guid lobbyId);
}