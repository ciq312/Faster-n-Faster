namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public record TransferHostCommand(Guid HostId, Guid LobbyId, Guid UserId);
