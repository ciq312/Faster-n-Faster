using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Auth.Services;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RequestPasswordReset;

public class RequestPasswordResetHandler(
    IUserRepository userRepo,
    IConfirmTokenIssuer tokenIssuer,
    IEmailSender emailSender) : IRequestHandler<RequestPasswordResetCommand>
{
    public async Task Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return;

        if (user.Password is null) return;

        var token = await tokenIssuer.TryIssue(user.Id, TokenType.PasswordReset);
        if (token is null) return;

        await emailSender.SendPasswordResetEmail(user.Nick, user.Email!, token.Value);
    }
}
