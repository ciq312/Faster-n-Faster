using FasterNFaster.Api.UseCases.Interfaces.Auth;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.Logout;

public class LogoutHandler(IRefreshTokenRepository refreshTokens, ISessionService sessions) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // The access token may already be expired, so the refresh cookie is the only reliable handle on the session.
        if (!string.IsNullOrEmpty(command.RefreshToken))
            await refreshTokens.Invalidate(command.RefreshToken);

        if (command.UserId is { } userId)
            await sessions.RevokeAllSessions(userId);
    }
}
