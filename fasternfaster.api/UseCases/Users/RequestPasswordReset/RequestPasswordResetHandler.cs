using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RequestPasswordReset;

public class RequestPasswordResetHandler(
    IUserRepository userRepo,
    IConfirmTokenRepository tokenRepo,
    IConfirmTokenFactory tokenFactory,
    IEmailSender emailSender) : IRequestHandler<RequestPasswordResetCommand>
{
    private readonly IUserRepository userRepo = userRepo;
    private readonly IConfirmTokenRepository tokenRepo = tokenRepo;
    private readonly IConfirmTokenFactory tokenFactory = tokenFactory;
    private readonly IEmailSender emailSender = emailSender;

    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(15);

    public async Task Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return;
    
        if (user.Password is null) return;

        Token? latest = await tokenRepo.GetLatestForUserAsync(user.Id, TokenType.PasswordReset);
        if (latest is not null && DateTime.UtcNow - latest.CreatedAt < ResendCooldown) return;

        await tokenRepo.RemoveAllForUser(user.Id, TokenType.PasswordReset);

        Token token = tokenFactory.GetToken(user.Id, TokenType.PasswordReset);
        await tokenRepo.Add(token);
        await tokenRepo.SaveChangesAsync();

        await emailSender.SendPasswordResetEmail(user.Nick, user.Email!, token.Value);
    }
}
