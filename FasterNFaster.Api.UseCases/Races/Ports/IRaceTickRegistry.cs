namespace FasterNFaster.Api.UseCases.Races.Ports;

public interface IRaceTickRegistry
{
    void RegisterLobby(Guid lobbyId);
    void DeregisterLobby(Guid lobbyId);
    void TransitionToRacing(Guid lobbyId);
    IReadOnlyList<RacingLobbyEntry> GetRacingLobbies();
}
