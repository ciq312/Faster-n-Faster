using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Events;

public record PlayerFinishedEvent(
    Guid LobbyId,
    string Nick,
    Guid PlayerId,
    int? FinishPosition,
    double Wpm,
    double Accuracy) : IDomainEvent;