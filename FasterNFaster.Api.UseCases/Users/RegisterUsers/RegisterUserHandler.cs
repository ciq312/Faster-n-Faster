using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RegisterUsers;

public class RegisterUserHandler(IUserRepository repo, IPasswordHelper passwordHelper, IEmailSender emailSender, IConfirmTokenRepository tokenRepo, IConfirmTokenFactory tokenFactory) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IConfirmTokenFactory tokenFactory = tokenFactory;
    private readonly IConfirmTokenRepository tokenRepo = tokenRepo;
    private readonly IUserRepository repo = repo;
    private readonly IPasswordHelper passwordHelper = passwordHelper;
    private readonly IEmailSender emailSender = emailSender;

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (await repo.GetUserByLoginAsync(command.Login) != null) throw new DuplicateLoginException(command.Login);
        if (await repo.GetByEmailAsync(command.Email) != null) throw new DuplicateEmailException(command.Email);

        User user = new(command.Nick, command.Login, command.Password);
        user.SetEmail(command.Email);

        string hashedPassword = passwordHelper.HashPassword(user, command.Password);
        user.SetPassword(hashedPassword);

        await repo.AddAsync(user);

        var verificationToken = tokenFactory.GetToken(user.Id, TokenType.EmailVerification);
        await tokenRepo.Add(verificationToken);

        await emailSender.SendConfirmationEmail(user.Nick, user.Email!, verificationToken.Value);

        return new RegisterUserResult(user.Id, user.Nick);
    }
}
