using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using MediatR;
using FasterNFaster.Api.UseCases.Interfaces.Db;

namespace FasterNFaster.Api.UseCases.Users.ResetPassword;

public class ResetPasswordHandler(
    IUserRepository userRepo,
    IUnitOfWork unitOfWork,
    IConfirmTokenRepository tokenRepo,
    IPasswordHelper passwordHelper,
    ISessionService sessionService) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        Token? token = await tokenRepo.GetByValueAsync(command.Token);
        if (token is null) throw new TokenNotFoundException(command.Token);
        if (token.Type != TokenType.PasswordReset) throw new TokenNotFoundException(command.Token);
        if (!token.IsValid()) throw new TokenNotFoundException(command.Token);

        User user = await userRepo.GetByIdAsync(token.UserId)
            ?? throw new UserNotFoundException(token.UserId);

        string hashedPassword = passwordHelper.HashPassword(user, command.NewPassword);
        user.SetPassword(hashedPassword);

        userRepo.Update(user);
        await unitOfWork.SaveChangesAsync();

        await tokenRepo.Remove(token);
        sessionService.ClearActiveSession(user.Id);
    }
}
