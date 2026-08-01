using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public record UpdateProgressCommand(Guid UserId, int Index, int Mistakes, string Typed) : IRequest;
