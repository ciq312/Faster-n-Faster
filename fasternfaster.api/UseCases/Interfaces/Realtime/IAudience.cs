namespace FasterNFaster.Api.UseCases.Interfaces.Realtime;

public interface IAudience { }

public record LobbyAudience(Guid LobbyId) : IAudience;
public record PlayerAudience(Guid UserId) : IAudience;
public record LobbyExceptAudience(Guid LobbyId, Guid UserId) : IAudience;

public static class Audience
{
    public static IAudience Lobby(Guid lobbyId) => new LobbyAudience(lobbyId);
    public static IAudience Player(Guid userId) => new PlayerAudience(userId);
    public static IAudience LobbyExcept(Guid lobbyId, Guid userId) => new LobbyExceptAudience(lobbyId, userId);
}
