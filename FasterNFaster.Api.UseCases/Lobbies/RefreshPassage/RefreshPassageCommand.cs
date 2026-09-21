using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public record RefreshPassageCommand(Guid CallerId) : IRequest;
