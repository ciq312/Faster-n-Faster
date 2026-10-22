using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerJoined;

[TranspilationSource]
public record PlayerJoinedDTO(Guid PlayerId, string DisplayName);
