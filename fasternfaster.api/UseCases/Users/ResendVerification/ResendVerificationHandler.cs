using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ResendVerification;

public class ResendVerificationHandler(
    IUserRepository userRepo,
    IConfirmTokenRepository tokenRepo,
    IConfirmTokenFactory tokenFactory,
    IEmailSender emailSender,
    ResendVerificationOptions options) : IRequestHandler<ResendVerificationCommand>
{
    private readonly IUserRepository userRepo = userRepo;
    private readonly IConfirmTokenRepository tokenRepo = tokenRepo;
    private readonly IConfirmTokenFactory tokenFactory = tokenFactory;
    private readonly IEmailSender emailSender = emailSender;
    private readonly ResendVerificationOptions options = options;


    public async Task Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return;
        if (user.IsEmailVerified) return;

        Token? latest = await tokenRepo.GetLatestForUserAsync(user.Id, TokenType.EmailVerification);

        if (latest is not null && IsWithinCooldown(latest)) return;

        await tokenRepo.RemoveAllForUser(user.Id, TokenType.EmailVerification);

        Token token = tokenFactory.GetToken(user.Id, TokenType.EmailVerification);
        await tokenRepo.Add(token);
        await tokenRepo.SaveChangesAsync();

        await emailSender.SendConfirmationEmail(user.Nick, user.Email!, token.Value);
    }

    private bool IsWithinCooldown(Token token) => DateTime.UtcNow - token.CreatedAt < options.Cooldown;
}
