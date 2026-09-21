namespace FasterNFaster.Api.UseCases.Interfaces.Realtime;

public static class Audience
{
    public static IAudience Lobby(Guid lobbyId) => new LobbyAudience(lobbyId);
    public static IAudience Player(Guid userId) => new PlayerAudience(userId);
    public static IAudience LobbyExcept(Guid lobbyId, Guid userId) => new LobbyExceptAudience(lobbyId, userId);
}
