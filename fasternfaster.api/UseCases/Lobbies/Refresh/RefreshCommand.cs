namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public record RefreshCommand(Guid LobbyId, Guid UserId);