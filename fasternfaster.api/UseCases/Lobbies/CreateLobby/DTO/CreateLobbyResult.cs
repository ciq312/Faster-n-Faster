namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby;

public record CreateLobbyResult(Guid LobbyId, string? inviteCode);
