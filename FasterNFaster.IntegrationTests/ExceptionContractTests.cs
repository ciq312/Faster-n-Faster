using System.Net.Http.Json;
using FasterNFaster.Api.Presentation.Web.Auth.RegisterUser;
using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Auth.Exceptions;
using FasterNFaster.Api.UseCases.Users.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Shouldly;
namespace FasterNFaster.IntegrationTests;

public class ExceptionContractTests(TestApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<TestApplicationFactory<Program>>
{
    private readonly Mock<IUserRepository> userRepo = new();

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.AddSingleton(userRepo.Object);
        });
    }
    [Fact]
    public async Task EmptyPassword_ShouldThrow400()
    {
        var client = App.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "");

        var response = await client.PostAsJsonAsync(AuthHelper.RegisterUri, user);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        var apiErrorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        apiErrorResponse.ShouldNotBeNull();
        apiErrorResponse.Reason.ShouldBe("One or more validation errors occurred.");
        apiErrorResponse.Code.ShouldBe("validation_failed");
        apiErrorResponse.Errors.ShouldNotBeNull();
        apiErrorResponse.Errors["password"].ShouldContain("Password can't be empty");
    }

    [Fact]
    public async Task ThrowsDomainError_ShouldReturnContractResponse()
    {
        var client = App.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "password123");

        userRepo
            .Setup(r => r.GetUserByLoginAsync(It.IsAny<string>()))
            .ThrowsAsync(new EmailNotVerifiedException("player@test.com"));

        var response = await client.PostAsJsonAsync(AuthHelper.RegisterUri, user);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
        var apiErrorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        apiErrorResponse.ShouldNotBeNull();
        apiErrorResponse.Reason.ShouldBe("Email verification required");
        apiErrorResponse.Code.ShouldBe("email_not_verified");
        apiErrorResponse.Errors.ShouldNotBeNull();
        apiErrorResponse.Errors["email"].ShouldContain("player@test.com");
    }

    [Fact]
    public async Task ThrowsUnknownError_ShouldReturnFallbackMessage()
    {
        var client = App.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "password123");

        userRepo
            .Setup(r => r.GetUserByLoginAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("some error"));

        var response = await client.PostAsJsonAsync(AuthHelper.RegisterUri, user);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.InternalServerError);
        var apiErrorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        apiErrorResponse.ShouldNotBeNull();
        apiErrorResponse.Reason.ShouldBe("An unexpected error occurred");
        apiErrorResponse.Code.ShouldBe("error");
        apiErrorResponse.Errors.ShouldBeNull();
    }

}
