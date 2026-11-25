using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.Logout;

public class LogoutHandler(
    IRefreshTokenRepository refreshTokens,
    ISessionService sessions,
    ILogger<LogoutHandler> logger
    ) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // The access token may already be expired, so the refresh cookie is the only reliable handle on the session.
        if (!string.IsNullOrEmpty(command.RefreshToken))
            await refreshTokens.Invalidate(command.RefreshToken);

        if (command.UserId is { } userId)
            await sessions.RevokeAllSessions(userId);

        logger.LogInformation("Revoked credentials for user {UserId} {TelemetryOperation}", command.UserId, TelemetryOperations.UserCredentialsRevoked);
    }
}
