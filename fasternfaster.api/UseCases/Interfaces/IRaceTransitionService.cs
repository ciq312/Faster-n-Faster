namespace FasternFaster.Api.UseCases.Interfaces;

public interface IRaceTransitionService
{
    public Task StartRaceInternal(Guid lobbyId);
}