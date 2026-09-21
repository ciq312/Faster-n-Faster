using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RequestPasswordReset;

public class RequestPasswordResetHandler(
    IUserRepository userRepo,
    IConfirmTokenIssuer tokenIssuer,
    IEmailSender emailSender) : IRequestHandler<RequestPasswordResetCommand>
{
    public async Task Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return;   
        
        if (user.Password is null) return;

        Token? token = await tokenIssuer.TryIssue(user.Id, TokenType.PasswordReset);
        if (token is null) return;

        await emailSender.SendPasswordResetEmail(user.Nick, user.Email!, token.Value);
    }
}
