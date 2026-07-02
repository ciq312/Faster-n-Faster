namespace FasternFaster.Api.UseCases.Interfaces;

public interface IRaceTransitionService
{
    public void StartRaceInternal(Guid lobbyId);
}