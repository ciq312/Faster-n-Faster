using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.ChangeNick;
using FasterNFaster.Api.UseCases.Users.Ports;
using FasterNFaster.UnitTests.Fakes;
using Moq;
using Shouldly;

namespace FasterNFaster.UnitTests.Users;

public class ChangeNickHandlerTests
{
    private readonly Mock<IUserRepository> userRepo = new();
    private readonly Mock<IUnitOfWork> unitOfWork = new();
    private readonly Mock<ITokenService> tokenService = new();

    private ChangeNickHandler CreateHandler() => new(userRepo.Object, unitOfWork.Object, tokenService.Object);

    [Fact]
    public async Task ExistingUser_SavesNickAndIssuesTokensCarryingIt()
    {
        var user = new User("OldNick", "login", "pass");
        var issued = new TokenPair(FakeTokenService.Token("access"), FakeTokenService.Token("refresh"));
        userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        tokenService.Setup(t => t.IssuePlayerTokens(user.Id, "NewNick")).ReturnsAsync(issued);

        var result = await CreateHandler().Handle(new ChangeNickCommand(user.Id, "NewNick"), CancellationToken.None);

        user.Nick.ShouldBe("NewNick");
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        result.Tokens.ShouldBe(issued);
    }

    [Fact]
    public async Task UnknownUser_ThrowsNotFound()
    {
        await Should.ThrowAsync<UserNotFoundException>(
            () => CreateHandler().Handle(new ChangeNickCommand(Guid.NewGuid(), "NewNick"), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
