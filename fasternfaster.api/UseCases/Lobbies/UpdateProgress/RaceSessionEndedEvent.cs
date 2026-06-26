using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public record RaceSessionEndedEvent(Lobby Lobby, IEnumerable<RaceParticipantResult> Results) : INotification;
