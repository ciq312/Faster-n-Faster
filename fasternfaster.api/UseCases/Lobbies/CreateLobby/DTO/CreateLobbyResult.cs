namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

public record CreateLobbyResult(Guid LobbyId, string? inviteCode);
