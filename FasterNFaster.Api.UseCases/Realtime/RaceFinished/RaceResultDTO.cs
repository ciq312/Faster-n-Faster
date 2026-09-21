namespace FasterNFaster.Api.UseCases.Realtime.RaceFinished;

public record RaceResultDTO(Guid PlayerId, string Nick, int? FinishPosition, float Wpm, float Accuracy, int MistakeCount);
