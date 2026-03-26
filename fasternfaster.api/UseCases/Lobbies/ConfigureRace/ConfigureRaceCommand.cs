namespace FasterNFaster.Api.UseCases.Lobbies.ConfigureRace;

public record ConfigureRaceCommand(Guid UserId, Guid LobbyId, string GameMode, int? WordCount, int? TimerDuration);
