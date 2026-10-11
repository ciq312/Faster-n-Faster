using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Auth.Services;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.ResendVerification;

public class ResendVerificationHandler(
    IUserRepository userRepo,
    IConfirmTokenIssuer tokenIssuer,
    IEmailSender emailSender) : IRequestHandler<ResendVerificationCommand>
{
    public async Task Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return;
        if (user.IsEmailVerified) return;

        var token = await tokenIssuer.TryIssue(user.Id, TokenType.EmailVerification);
        if (token is null) return;

        await emailSender.SendConfirmationEmail(user.Nick, user.Email!, token.Value);
    }
}
