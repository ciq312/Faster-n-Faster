using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ResendVerification;

public class ResendVerificationHandler(
    IUserRepository userRepo,
    IConfirmTokenIssuer tokenIssuer,
    IEmailSender emailSender,
    ResendVerificationOptions options) : IRequestHandler<ResendVerificationCommand>
{
    public async Task Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return;
        if (user.IsEmailVerified) return;

        Token? token = await tokenIssuer.TryIssue(user.Id, TokenType.EmailVerification, options.Cooldown);
        if (token is null) return;

        await emailSender.SendConfirmationEmail(user.Nick, user.Email!, token.Value);
    }
}
