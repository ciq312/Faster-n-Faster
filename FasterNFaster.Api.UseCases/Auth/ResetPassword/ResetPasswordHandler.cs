using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.ResetPassword;

public class ResetPasswordHandler(
    IUserRepository userRepo,
    IUnitOfWork unitOfWork,
    IConfirmTokenRepository tokenRepo,
    IPasswordHelper passwordHelper,
    ISessionService sessionService,
    TimeProvider timeProvider) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var token = await tokenRepo.GetByValueAsync(command.Token);
        if (token is null) throw new TokenNotFoundException();
        if (token.Type != TokenType.PasswordReset) throw new TokenNotFoundException();
        if (!token.IsValid(timeProvider.GetUtcNow())) throw new TokenNotFoundException();

        var user = await userRepo.GetByIdAsync(token.UserId)
            ?? throw new UserNotFoundException(token.UserId);

        var hashedPassword = passwordHelper.HashPassword(user, command.NewPassword);
        user.SetPassword(hashedPassword);

        userRepo.Update(user);
        await unitOfWork.SaveChangesAsync();

        await tokenRepo.Remove(token);
        await sessionService.RevokeAllSessions(user.Id);
    }
}
