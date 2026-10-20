using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;
