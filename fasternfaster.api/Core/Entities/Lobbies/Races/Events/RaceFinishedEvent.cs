using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces.Events;

public record RaceFinishedEvent(Guid lobbyId, IEnumerable<RaceParticipantResult> results) : IDomainEvent;