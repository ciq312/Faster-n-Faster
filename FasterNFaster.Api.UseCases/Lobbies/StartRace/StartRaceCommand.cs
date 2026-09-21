using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public record StartRaceCommand(Guid UserId) : IRequest<Guid>;
