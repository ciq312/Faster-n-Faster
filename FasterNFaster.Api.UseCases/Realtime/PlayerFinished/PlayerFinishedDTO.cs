using Tapper;

namespace FasterNFaster.Api.UseCases.Realtime.PlayerFinished;

[TranspilationSource]
public record PlayerFinishedDTO(string Nick, Guid PlayerId, int? FinishPosition, double Wpm, double Accuracy);
