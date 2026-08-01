namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public record RacingLobbyEntry(Guid LobbyId, RacePhase Phase, DateTime RegisteredAt);
