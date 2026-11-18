using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerPromoted;

[TranspilationSource]
public record HostChangedDTO(Guid UserId, string NewHostNick);
