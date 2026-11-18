using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerDisconnected;

[TranspilationSource]
public record PlayerDisconnectedDTO(Guid DisconnectedUserId, string DisconnectedUserNick);
