namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceTransitionService
{
    public Task StartRaceInternal(Guid lobbyId);
}