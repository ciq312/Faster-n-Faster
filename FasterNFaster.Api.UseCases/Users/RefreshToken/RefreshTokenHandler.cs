using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RefreshToken;

public class RefreshTokenHandler(ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly ITokenService tokenService = tokenService;

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokens = await tokenService.TryRefreshTokens(command.RefreshToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        return new RefreshTokenResult(tokens);
    }
}
