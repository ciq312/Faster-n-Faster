namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public enum RacePhase { Countdown, Racing }

public record RacingLobbyEntry(Guid LobbyId, RacePhase Phase, DateTime RegisteredAt);

public interface IRaceTickRegistry
{
    void RegisterLobby(Guid lobbyId);
    void DeregisterLobby(Guid lobbyId);
    void TransitionToRacing(Guid lobbyId);
    IReadOnlyList<RacingLobbyEntry> GetRacingLobbies();
}
