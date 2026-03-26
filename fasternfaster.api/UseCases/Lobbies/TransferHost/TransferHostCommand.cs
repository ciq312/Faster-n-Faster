namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public record TransferHostCommand(Guid UserId, Guid LobbyId, Guid TargetPlayerId);
