namespace FasterNFaster.Api.UseCases.Interfaces.Realtime;

public interface IAudience { }

public record LobbyAudience(Guid LobbyId) : IAudience;
public record PlayerAudience(Guid UserId) : IAudience;
public record LobbyExceptAudience(Guid LobbyId, Guid UserId) : IAudience;
