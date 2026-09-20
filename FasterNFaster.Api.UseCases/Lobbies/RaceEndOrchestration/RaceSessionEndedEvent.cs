using FasterNFaster.Api.Core.Entities.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public record RaceSessionEndedEvent(Guid LobbyId, IEnumerable<RaceParticipantResult> Results) : INotification;
