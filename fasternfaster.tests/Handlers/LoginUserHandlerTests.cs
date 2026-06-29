using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Auth;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Handlers;

public class LoginUserHandlerTests
{
    [Fact]
    public async Task LoginNotFound_ShouldThrowKeyNotFound()
    {
        var repo = new FakeUserRepository();

        var tokenStore = new InMemoryRefreshTokenRepository();

        var jwtOptions = Options.Create(new JwtOptions
        {
            RefreshTokenLifetime = TimeSpan.FromHours(1)
        });

        var tokenFactory = new JwtTokenFactory(jwtOptions);

        var tokenService = new JwtTokenService(tokenFactory, tokenStore, repo, jwtOptions);
        var handler = new LoginUserHandler(repo, PasswordHelperFactory.Create(), tokenService);


        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginUserCommand("noone", "pass123"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task WrongPassword_ShouldThrowInvalidData()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User("Player1", "mylogin", "correctpass"));


        var tokenStore = new InMemoryRefreshTokenRepository();

        var jwtOptions = Options.Create(new JwtOptions
        {
            RefreshTokenLifetime = TimeSpan.FromHours(1)
        });

        var tokenFactory = new JwtTokenFactory(jwtOptions);

        var tokenService = new JwtTokenService(tokenFactory, tokenStore, repo, jwtOptions);

        var handler = new LoginUserHandler(repo, PasswordHelperFactory.Create(), tokenService);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginUserCommand("mylogin", "wrongpass"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task CorrectCredentials_ShouldReturnTokenAndId()
    {
        var repo = new FakeUserRepository();
        var user = new User("Player1", "mylogin", "pass123");
        user.SetEmailVerified();
        repo.Seed(user);


        var tokenStore = new InMemoryRefreshTokenRepository();

        var jwtOptions = Options.Create(new JwtOptions
        {
            RefreshTokenLifetime = TimeSpan.FromHours(1)
        });

        var tokenFactory = new JwtTokenFactory(jwtOptions);

        var tokenService = new JwtTokenService(tokenFactory, tokenStore, repo, jwtOptions);

        var handler = new LoginUserHandler(repo, PasswordHelperFactory.Create(), tokenService);

        var result = await handler.Handle(new LoginUserCommand("mylogin", "pass123"), CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
    }
}
