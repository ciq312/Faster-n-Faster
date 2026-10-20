using FasterNFaster.Api.Domain.Interfaces.Events;

namespace FasterNFaster.Api.Domain.Entities.Lobbies.Events;

public record PlayerFinishedEvent(
    Guid LobbyId,
    string Nick,
    Guid PlayerId,
    int? FinishPosition,
    double Wpm,
    double Accuracy) : IDomainEvent;
