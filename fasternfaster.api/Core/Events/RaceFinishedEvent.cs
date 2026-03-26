using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Events;

public record RaceFinishedPlayerData(Guid PlayerId, int? FinishPosition, double Wpm, double Accuracy, int Mistakes);

public record RaceFinishedEvent(
    Guid LobbyId,
    IReadOnlyList<RaceFinishedPlayerData> Results) : IDomainEvent;
