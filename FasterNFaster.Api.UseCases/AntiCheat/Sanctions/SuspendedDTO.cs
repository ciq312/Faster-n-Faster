using Tapper;

namespace FasterNFaster.Api.UseCases.AntiCheat.Sanctions;

[TranspilationSource]
public record SuspendedDTO(DateTimeOffset ExpiresAt);
