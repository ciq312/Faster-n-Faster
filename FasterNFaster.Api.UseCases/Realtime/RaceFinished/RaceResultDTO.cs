using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.RaceFinished;

[TranspilationSource]
public record RaceResultDTO(Guid PlayerId, string Nick, int? FinishPosition, float Wpm, float Accuracy, int MistakeCount);
