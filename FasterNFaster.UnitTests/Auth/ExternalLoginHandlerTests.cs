using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.UseCases.Auth.ExternalLogin;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Ports;
using Moq;
using Shouldly;

namespace FasterNFaster.UnitTests.Auth;

public class ExternalLoginHandlerTests
{
    private const string Provider = "google";
    private const string ExistingEmail = "user@mail.com";
    private const string NewEmail = "new@mail.com";

    private readonly Mock<IUserRepository> userRepo = new();
    private readonly Mock<IExternalLoginRepository> externalLogins = new();
    private readonly Mock<ITokenService> tokenService = new();
    private readonly Mock<IUnitOfWork> unitOfWork = new();
    private readonly List<User> addedUsers = [];

    public ExternalLoginHandlerTests() => userRepo.Setup(r => r.Add(Capture.In(addedUsers)));

    private ExternalLoginHandler CreateHandler() =>
        new(userRepo.Object, externalLogins.Object, tokenService.Object, unitOfWork.Object);

    private static User ExistingUser()
    {
        var user = new User("Existing", "login", "pass");
        user.SetEmail(ExistingEmail);
        return user;
    }

    private static ExternalLoginCommand Command(string subject, string email, bool emailVerified = true) =>
        new(Provider, subject, email, emailVerified);

    private void SetupLinkedLogin(Guid userId, string subject) =>
        externalLogins
            .Setup(r => r.GetByProviderAndSubjectAsync(Provider, subject))
            .ReturnsAsync(new ExternalLogin(userId, subject, ExistingEmail, Provider));

    [Fact]
    public async Task ExistingExternalLogin_ResolvesLinkedUser_WithoutCreatingAnotherLink()
    {
        var user = ExistingUser();
        SetupLinkedLogin(user.Id, "sub-1");
        userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await CreateHandler().Handle(Command("sub-1", ExistingEmail), CancellationToken.None);

        tokenService.Verify(t => t.IssuePlayerTokens(user.Id, user.Nick), Times.Once);
        externalLogins.Verify(
            r => r.Add(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task NoLinkButVerifiedEmailMatches_LinksToExistingAccount()
    {
        var user = ExistingUser();
        userRepo.Setup(r => r.GetByEmailAsync(ExistingEmail)).ReturnsAsync(user);

        await CreateHandler().Handle(Command("sub-2", ExistingEmail), CancellationToken.None);

        tokenService.Verify(t => t.IssuePlayerTokens(user.Id, user.Nick), Times.Once);
        externalLogins.Verify(r => r.Add(user.Id, Provider, "sub-2", ExistingEmail), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        addedUsers.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoMatch_CreatesNewVerifiedUserAndLink()
    {
        await CreateHandler().Handle(Command("sub-3", NewEmail), CancellationToken.None);

        var created = addedUsers.ShouldHaveSingleItem();
        created.Email.ShouldBe(NewEmail);
        created.IsEmailVerified.ShouldBeTrue();
        tokenService.Verify(t => t.IssuePlayerTokens(created.Id, created.Nick), Times.Once);
        externalLogins.Verify(r => r.Add(created.Id, Provider, "sub-3", NewEmail), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UnverifiedEmail_DoesNotLinkToExistingAccount_CreatesNewUser()
    {
        var existing = ExistingUser();
        userRepo.Setup(r => r.GetByEmailAsync(ExistingEmail)).ReturnsAsync(existing);

        await CreateHandler().Handle(Command("sub-4", ExistingEmail, emailVerified: false), CancellationToken.None);

        var created = addedUsers.ShouldHaveSingleItem();
        created.Id.ShouldNotBe(existing.Id);
        tokenService.Verify(t => t.IssuePlayerTokens(created.Id, created.Nick), Times.Once);
    }

    [Fact]
    public async Task NoMatch_GivesDefaultNick_AndIsReportedAsNew()
    {
        var result = await CreateHandler().Handle(Command("sub-6", NewEmail), CancellationToken.None);

        addedUsers.ShouldHaveSingleItem().Nick.ShouldMatch(@"^player_\d{4}$");
        result.IsNewUser.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistingExternalLogin_IsNotReportedAsNew()
    {
        var user = ExistingUser();
        SetupLinkedLogin(user.Id, "sub-7");
        userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await CreateHandler().Handle(Command("sub-7", ExistingEmail), CancellationToken.None);

        result.IsNewUser.ShouldBeFalse();
    }

    [Fact]
    public async Task NoLinkButVerifiedEmailMatches_KeepsNick_AndIsNotReportedAsNew()
    {
        var user = ExistingUser();
        userRepo.Setup(r => r.GetByEmailAsync(ExistingEmail)).ReturnsAsync(user);

        var result = await CreateHandler().Handle(Command("sub-8", ExistingEmail), CancellationToken.None);

        user.Nick.ShouldBe("Existing");
        result.IsNewUser.ShouldBeFalse();
    }

    [Fact]
    public async Task ExternalLoginPointsToMissingUser_Throws()
    {
        SetupLinkedLogin(Guid.NewGuid(), "sub-5");

        await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler().Handle(Command("sub-5", ExistingEmail), CancellationToken.None));
    }
}
