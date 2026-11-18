using MediatR;

namespace FasterNFaster.Api.UseCases.Races.GetRaceProgress;

public sealed record GetRaceProgressQuery(Guid UserId) : IRequest<RaceProgressDTO?>;
