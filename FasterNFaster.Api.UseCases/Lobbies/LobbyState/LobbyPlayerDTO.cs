namespace FasterNFaster.Api.UseCases.Lobbies.LobbyState;

public record LobbyPlayerDTO(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected, string Color);
