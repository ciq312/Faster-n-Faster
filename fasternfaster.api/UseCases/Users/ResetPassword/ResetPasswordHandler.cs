using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.UseCases.Users.ResetPassword;

public class ResetPasswordHandler(
    IUserRepository userRepo,
    ITokenRepository tokenRepo,
    IPasswordHelper passwordHelper,
    ISessionService sessionService
    ) : IHandler<ResetPasswordCommand>
{
    private readonly ISessionService sessionService = sessionService;
    private readonly IUserRepository userRepo = userRepo;
    private readonly ITokenRepository tokenRepo = tokenRepo;
    private readonly IPasswordHelper passwordHelper = passwordHelper;

    public async Task Handle(ResetPasswordCommand command)
    {
        Token? token = await tokenRepo.GetByValueAsync(command.Token);
        // Type mismatch / expired are bucketed with "not found" on purpose —
        // we never tell the caller which of the three it was.
        if (token is null) throw new TokenNotFoundException(command.Token);
        if (token.Type != TokenType.PasswordReset) throw new TokenNotFoundException(command.Token);
        if (!token.TryVerify()) throw new TokenNotFoundException(command.Token);

        User user = await userRepo.GetByIdAsync(token.UserId)
            ?? throw new UserNotFoundException(token.UserId);

        string hashedPassword = passwordHelper.HashPassword(user, command.NewPassword);
        user.SetPassword(hashedPassword);

        // Single-use-by-deletion. Nuke every outstanding reset token for this user,
        // not just the consumed one — stale tokens shouldn't survive a successful reset.
        sessionService.ClearActiveSession(user.Id);
        await tokenRepo.RemoveAllForUser(user.Id, TokenType.PasswordReset);
        await tokenRepo.SaveChangesAsync();

    }
}
