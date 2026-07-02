using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Users.ExternalLogin.Commands;
using FasterNFaster.Api.UseCases.Users.ExternalLogin.DTO;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ExternalLogin.Handlers;

public class ExternalLoginHandler(
    IUserRepository userRepo,
    IExternalLoginRepository externalLogins,
    ITokenService tokenService) : IRequestHandler<ExternalLoginCommand, ExternalLoginResult>
{
    private readonly IUserRepository userRepo = userRepo;
    private readonly IExternalLoginRepository externalLogins = externalLogins;
    private readonly ITokenService tokenService = tokenService;

    public async Task<ExternalLoginResult> Handle(ExternalLoginCommand command, CancellationToken cancellationToken)
    {
        var user = await GetLinkedUserAsync(command)
                   ?? await LinkToExistingAccountAsync(command)
                   ?? await RegisterNewUserAsync(command);

        var tokens = await tokenService.IssuePlayerTokens(user.Id, user.Nick);

        return new ExternalLoginResult(tokens);
    }

    private async Task<User?> GetLinkedUserAsync(ExternalLoginCommand command)
    {
        var login = await externalLogins.GetByProviderAndSubjectAsync(command.Provider, command.Subject);
        if (login is null) return null;

        return await userRepo.GetByIdAsync(login.UserId)
            ?? throw new InvalidOperationException($"External login references missing user {login.UserId}.");
    }

    private async Task<User?> LinkToExistingAccountAsync(ExternalLoginCommand command)
    {
        if (!command.EmailVerified) return null;

        var user = await userRepo.GetByEmailAsync(command.Email);
        if (user is null) return null;

        await externalLogins.AddAsync(user.Id, command.Provider, command.Subject, command.Email);
        return user;
    }

    private async Task<User> RegisterNewUserAsync(ExternalLoginCommand command)
    {
        var user = new User(command.Name, null, null);
        user.SetEmail(command.Email);
        user.SetEmailVerified();

        await userRepo.AddAsync(user);
        await externalLogins.AddAsync(user.Id, command.Provider, command.Subject, command.Email);

        return user;
    }
}
