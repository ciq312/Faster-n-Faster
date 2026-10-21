using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.RaceState;

[TranspilationSource]
public record RaceParticipantDTO(Guid PlayerId, int Index, string Typed, double Wpm, string Color, string Nick, int Mistakes);
