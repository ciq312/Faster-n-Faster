using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.RaceState;

[TranspilationSource]
public record RaceParticipantDTO(Guid PlayerId, int Index, double Wpm);
