using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerKicked;

[TranspilationSource]
public record PlayerKickedDTO(Guid UserId, string Nick);
