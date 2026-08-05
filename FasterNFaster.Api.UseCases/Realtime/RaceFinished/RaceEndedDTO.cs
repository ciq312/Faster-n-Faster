using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.Realtime.RaceFinished;

public record RaceEndedDTO(IEnumerable<RaceParticipantResult> Results);
