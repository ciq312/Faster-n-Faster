using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RegisterAnonymous;

public class RegisterAnonymousHandler(
    ITokenService tokenService,
    ILogger<RegisterAnonymousHandler> logger
    ) : IRequestHandler<RegisterAnonymousCommand, RegisterAnonymousResult>
{
    public Task<RegisterAnonymousResult> Handle(RegisterAnonymousCommand command, CancellationToken cancellationToken)
    {
        var guestId = Guid.NewGuid();
        var accessToken = tokenService.IssueGuestTokens(guestId, command.Nick);

        logger.LogInformation("User {UserId} registered anonymously with nick {Nick} {TelemetryOperation}", guestId, command.Nick, TelemetryOperations.UserRegisteredAnonymously);

        return Task.FromResult(new RegisterAnonymousResult(command.Nick, guestId, accessToken));
    }
}
