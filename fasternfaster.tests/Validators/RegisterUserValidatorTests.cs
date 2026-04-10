using FasterNFaster.Api.Web.Users.RegisterUser;
using FluentValidation.TestHelper;

namespace FasterNFaster.Tests.Validators;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator validator = new();

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        var request = new RegisterUserRequest { Nick = "Player1", Login = "mylogin", Password = "pass123" };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "mylogin", "pass123")]           // empty nick
    [InlineData("   ", "mylogin", "pass123")]         // whitespace nick
    [InlineData("Player1", "", "pass123")]            // empty login
    [InlineData("Player1", "mylogin", "")]            // empty password
    [InlineData("Player1", "mylogin", "ab")]          // password too short
    public void InvalidData_ShouldFail(string nick, string login, string password)
    {
        var request = new RegisterUserRequest { Nick = nick, Login = login, Password = password };
        var result = validator.TestValidate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(11, 5, 5)]    // nick over max
    [InlineData(5, 11, 5)]    // login over max
    [InlineData(5, 5, 31)]    // password over max
    public void OverMaxLength_ShouldFail(int nickLen, int loginLen, int passLen)
    {
        var request = new RegisterUserRequest
        {
            Nick = new string('a', nickLen),
            Login = new string('a', loginLen),
            Password = new string('a', passLen)
        };
        var result = validator.TestValidate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(10, 4, 4)]    // all at boundary minimums/maximums
    [InlineData(1, 10, 10)]   // short nick is fine
    [InlineData(10, 10, 30)]  // all at max
    public void BoundaryValues_ShouldPass(int nickLen, int loginLen, int passLen)
    {
        var request = new RegisterUserRequest
        {
            Nick = new string('a', nickLen),
            Login = new string('a', loginLen),
            Password = new string('a', passLen)
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
