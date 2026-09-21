using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public record ChangeColorCommand(Guid UserId, string Color) : IRequest;
