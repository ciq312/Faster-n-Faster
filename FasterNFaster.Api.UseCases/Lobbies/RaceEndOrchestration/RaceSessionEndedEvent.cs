using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public record RaceSessionEndedEvent(Lobby Lobby, IEnumerable<RaceParticipantResult> Results) : INotification;
