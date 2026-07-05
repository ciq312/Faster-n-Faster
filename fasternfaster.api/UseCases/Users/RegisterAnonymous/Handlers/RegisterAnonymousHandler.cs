using FasterNFaster.Api.UseCases.Interfaces.Auth;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Handlers;

public class RegisterAnonymousHandler(ITokenService tokenService) : IRequestHandler<RegisterAnonymousCommand, RegisterAnonymousResult>
{
    private readonly ITokenService tokenService = tokenService;

    public Task<RegisterAnonymousResult> Handle(RegisterAnonymousCommand command, CancellationToken cancellationToken)
    {
        var guestId = Guid.NewGuid();
        var tokens = tokenService.IssueGuestTokens(guestId, command.Nick);

        return Task.FromResult(new RegisterAnonymousResult(command.Nick, guestId, tokens));
    }
}
