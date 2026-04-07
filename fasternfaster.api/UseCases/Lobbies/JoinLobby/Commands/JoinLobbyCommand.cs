namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;

public record JoinLobbyCommand(Guid PlayerId, Guid LobbyId, string? InviteCode = null);
