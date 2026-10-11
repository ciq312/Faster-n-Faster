using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RefreshToken;

public class RefreshTokenHandler(ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokens = await tokenService.TryRefreshTokens(command.RefreshToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        return new RefreshTokenResult(tokens);
    }
}
