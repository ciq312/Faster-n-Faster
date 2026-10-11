namespace FasterNFaster.Api.Domain.Exceptions.Lobbies;

public class LobbyInRaceException(string action) : ConflictException($"Can't {action} during a race");
