using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.RaceFinished;

[TranspilationSource]
public record RaceEndedDTO(IEnumerable<RaceResultDTO> Results);
