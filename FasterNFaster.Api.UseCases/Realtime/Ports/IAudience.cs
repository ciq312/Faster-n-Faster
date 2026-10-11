namespace FasterNFaster.Api.UseCases.Realtime.Ports;

public interface IAudience { }

public record LobbyAudience(Guid LobbyId) : IAudience;
public record PlayerAudience(Guid UserId) : IAudience;
public record LobbyExceptAudience(Guid LobbyId, Guid UserId) : IAudience;
public record PlayersAudience(IEnumerable<Guid> PlayerIds) : IAudience;
