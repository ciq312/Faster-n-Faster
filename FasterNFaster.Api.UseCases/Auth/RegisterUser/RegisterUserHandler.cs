using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.UseCases.Auth.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RegisterUser;

public class RegisterUserHandler(
    IUserRepository repo,
    IUnitOfWork unitOfWork,
    IPasswordHelper passwordHelper,
    IEmailSender emailSender,
    IConfirmTokenRepository tokenRepo,
    IConfirmTokenFactory tokenFactory) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (await repo.GetUserByLoginAsync(command.Login) != null) throw new DuplicateLoginException(command.Login);
        if (await repo.GetByEmailAsync(command.Email) != null) throw new DuplicateEmailException(command.Email);

        User user = new(command.Nick, command.Login, null);
        user.SetEmail(command.Email);

        var hashedPassword = passwordHelper.HashPassword(user, command.Password);
        user.SetPassword(hashedPassword);

        repo.Add(user);
        await unitOfWork.SaveChangesAsync();

        var verificationToken = tokenFactory.GetToken(user.Id, TokenType.EmailVerification);
        await tokenRepo.Add(verificationToken);

        await emailSender.SendConfirmationEmail(user.Nick, user.Email!, verificationToken.Value);

        return new RegisterUserResult(user.Id, user.Nick);
    }
}
