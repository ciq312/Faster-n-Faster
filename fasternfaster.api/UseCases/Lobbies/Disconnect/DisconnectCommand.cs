namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public record DisconnectCommand(string ConnectionId, Guid LobbyId, Guid PlayerId);
