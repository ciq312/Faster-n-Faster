using FasterNFaster.Api.UseCases.Auth.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RegisterAnonymous;

public class RegisterAnonymousHandler(ITokenService tokenService) : IRequestHandler<RegisterAnonymousCommand, RegisterAnonymousResult>
{
    public Task<RegisterAnonymousResult> Handle(RegisterAnonymousCommand command, CancellationToken cancellationToken)
    {
        var guestId = Guid.NewGuid();
        var tokens = tokenService.IssueGuestTokens(guestId, command.Nick);

        return Task.FromResult(new RegisterAnonymousResult(command.Nick, guestId, tokens));
    }
}
