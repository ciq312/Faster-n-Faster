namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceTickRegistry
{
    void RegisterLobby(Guid lobbyId);
    void DeregisterLobby(Guid lobbyId);
    void TransitionToRacing(Guid lobbyId);
    IReadOnlyList<RacingLobbyEntry> GetRacingLobbies();
}
