namespace FasterNFaster.Api.UseCases.Races.Ports;

public record RacingLobbyEntry(Guid LobbyId, RacePhase Phase, DateTime RegisteredAt);
