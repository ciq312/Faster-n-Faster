using Tapper;

namespace FasterNFaster.Api.UseCases.Lobbies.LobbyState;

[TranspilationSource]
public record LobbyPlayerDTO(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected, string Color);
