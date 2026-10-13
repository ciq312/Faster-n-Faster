namespace FasterNFaster.Api.Domain.Exceptions.Races;

public class PlayerNotInRaceException(Guid playerId) : NotFoundException($"Player {playerId} is not in this race");
