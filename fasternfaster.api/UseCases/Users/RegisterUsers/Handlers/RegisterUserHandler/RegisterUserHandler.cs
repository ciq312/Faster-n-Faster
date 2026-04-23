using System.Security.Cryptography;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.Infrastructure.Smtp.EmailSender;
using FasterNFaster.Api.Migrations;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Helpers.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.DTO;
using Microsoft.AspNetCore.Identity;

namespace FasterNFaster.Api.UseCases.Users.RegisterUsers.Handlers;

public class RegisterUserHandler(IUserRepository repo, IPasswordHelper passwordHelper, IEmailSender emailSender, ITokenRepository tokenRepo, ITokenFactory tokenFactory) : IHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly ITokenFactory tokenFactory = tokenFactory;
    private readonly ITokenRepository tokenRepo = tokenRepo;
    private readonly IUserRepository repo = repo;
    private readonly IPasswordHelper passwordHelper = passwordHelper;
    private readonly IEmailSender emailSender = emailSender;
    public async Task<RegisterUserResult> Handle(RegisterUserCommand command)
    {
        if (await repo.GetUserByLoginAsync(command.Login) != null) throw new DuplicateLoginException(command.Login);
        if (await repo.GetByEmailAsync(command.Email) != null) throw new DuplicateEmailException(command.Email);

        User user = new(command.Nick, command.Login, command.Password);
        user.SetEmail(command.Email);

        string hashedPassword = passwordHelper.HashPassword(user, command.Password);
        user.SetPassword(hashedPassword);

#if DEBUG
        Log.Information($"created user {user.Nick}");
#endif

        await repo.AddAsync(user);

        var verificationToken = tokenFactory.GetToken(user.Id, TokenType.EmailVerification);
        await tokenRepo.Add(verificationToken);
        await tokenRepo.SaveChangesAsync();

        await emailSender.SendConfirmationEmail(user.Nick, user.Email!, verificationToken.Value);

        return await Task.FromResult(new RegisterUserResult(user.Id, user.Nick));
    }
}