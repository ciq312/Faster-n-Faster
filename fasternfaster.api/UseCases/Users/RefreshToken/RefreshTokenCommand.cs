using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;
