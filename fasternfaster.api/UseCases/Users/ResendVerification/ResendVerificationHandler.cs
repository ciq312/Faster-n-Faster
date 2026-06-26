using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.Infrastructure.Smtp.EmailSender;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ResendVerification;

public class ResendVerificationHandler(
    IUserRepository userRepo,
    ITokenRepository tokenRepo,
    ITokenFactory tokenFactory,
    IEmailSender emailSender) : IRequestHandler<ResendVerificationCommand>
{
    private readonly IUserRepository userRepo = userRepo;
    private readonly ITokenRepository tokenRepo = tokenRepo;
    private readonly ITokenFactory tokenFactory = tokenFactory;
    private readonly IEmailSender emailSender = emailSender;

    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(15);

    public async Task Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return;
        if (user.IsEmailVerified) return;

        Token? latest = await tokenRepo.GetLatestForUserAsync(user.Id, TokenType.EmailVerification);
        if (latest is not null && DateTime.UtcNow - latest.CreatedAt < ResendCooldown) return;

        await tokenRepo.RemoveAllForUser(user.Id, TokenType.EmailVerification);

        Token token = tokenFactory.GetToken(user.Id, TokenType.EmailVerification);
        await tokenRepo.Add(token);
        await tokenRepo.SaveChangesAsync();

        await emailSender.SendConfirmationEmail(user.Nick, user.Email!, token.Value);
    }
}
