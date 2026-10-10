namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class LobbyInRaceException(string action) : ConflictException($"Can't {action} during a race");
