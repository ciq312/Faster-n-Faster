namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public record UpdateProgressCommand(Guid UserId, Guid LobbyId, int Index, int TotalTyped, int Mistakes);
