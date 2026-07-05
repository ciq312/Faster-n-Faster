namespace FasterNFaster.Api.UseCases.Realtime.PlayerFinished;

public record PlayerFinishedDTO(string Nick, Guid PlayerId, int? FinishPosition, double Wpm, double Accuracy);
