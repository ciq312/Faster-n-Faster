using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Users.ExternalLogin.Commands;
using FasterNFaster.Api.UseCases.Users.ExternalLogin.Handlers;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class ExternalLoginHandlerTests
{
    private const string Provider = "google";

    private readonly FakeUserRepository users = new();
    private readonly FakeExternalLoginRepository externalLogins = new();
    private readonly FakeTokenService tokenService = new();

    private ExternalLoginHandler CreateHandler() => new(users, externalLogins, tokenService);

    private static User SeedableUser(string email)
    {
        var user = new User("Existing", "login", "pass");
        user.SetEmail(email);
        return user;
    }

    private static ExternalLoginCommand Command(string subject, string email, bool emailVerified = true) =>
        new(Provider, subject, email, "Display Name", emailVerified);

    [Fact]
    public async Task ExistingExternalLogin_ResolvesLinkedUser_WithoutCreatingAnotherLink()
    {
        var user = SeedableUser("user@mail.com");
        users.Seed(user);
        await externalLogins.AddAsync(user.Id, Provider, "sub-1", "user@mail.com");

        await CreateHandler().Handle(Command("sub-1", "user@mail.com"), CancellationToken.None);

        Assert.Equal(user.Id, tokenService.IssuedForUserId);
        Assert.Single(externalLogins.Logins);
    }

    [Fact]
    public async Task NoLinkButVerifiedEmailMatches_LinksToExistingAccount()
    {
        var user = SeedableUser("user@mail.com");
        users.Seed(user);

        await CreateHandler().Handle(Command("sub-2", "user@mail.com"), CancellationToken.None);

        Assert.Equal(user.Id, tokenService.IssuedForUserId);
        var link = Assert.Single(externalLogins.Logins);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal("sub-2", link.ExternalSubject);
        Assert.Single(users.Users);
    }

    [Fact]
    public async Task NoMatch_CreatesNewVerifiedUserAndLink()
    {
        await CreateHandler().Handle(Command("sub-3", "new@mail.com"), CancellationToken.None);

        var created = Assert.Single(users.Users);
        Assert.Equal("new@mail.com", created.Email);
        Assert.True(created.IsEmailVerified);
        Assert.Equal(created.Id, tokenService.IssuedForUserId);
        Assert.Single(externalLogins.Logins);
    }

    [Fact]
    public async Task UnverifiedEmail_DoesNotLinkToExistingAccount_CreatesNewUser()
    {
        var existing = SeedableUser("user@mail.com");
        users.Seed(existing);

        await CreateHandler().Handle(Command("sub-4", "user@mail.com", emailVerified: false), CancellationToken.None);

        Assert.Equal(2, users.Users.Count);
        Assert.NotEqual(existing.Id, tokenService.IssuedForUserId);
    }

    [Fact]
    public async Task ExternalLoginPointsToMissingUser_Throws()
    {
        await externalLogins.AddAsync(Guid.NewGuid(), Provider, "sub-5", "ghost@mail.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateHandler().Handle(Command("sub-5", "ghost@mail.com"), CancellationToken.None));
    }
}
