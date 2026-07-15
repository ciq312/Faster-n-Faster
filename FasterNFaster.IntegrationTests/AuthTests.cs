using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Users.RegisterUsers;
using FasterNFaster.Api.Web.Lobbies.CreateLobby;
using FasterNFaster.Api.Web.Users.LoginUser;
using FasterNFaster.Api.Web.Users.RegisterUser;
using FasterNFaster.Api.Web.Users.RegisterUser.EndPoints;
using FasterNFaster.IntegrationTests;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Mvc.Testing;
using Org.BouncyCastle.Asn1;

public class AuthTests(NoRateLimitApplicationFactory<Program> factory) : IClassFixture<NoRateLimitApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly NoRateLimitApplicationFactory<Program> factory = factory;

    public Task InitializeAsync() => factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FullRegisterFlow_ShouldPersistUser()
    {
        var client = factory.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        var registerResponse = await client.PostAsJsonAsync(AuthHelper.RegisterUri, user);

        var result = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResult>();
        var userId = result!.UserId;

        var confirmEmailToken = await factory.ExecuteScopedAsync<IConfirmTokenRepository, Token?>(repo =>
        repo.GetLatestForUserAsync(
            userId,
            TokenType.EmailVerification));

        var verifyEmailResponse = await client.PostAsJsonAsync(
            AuthHelper.VerifyEmailUri,
            new VerifyEmailRequest(confirmEmailToken!.Value));

        var userDb = await factory.ExecuteScopedAsync<IUserRepository, User?>(
            repo => repo.GetByIdAsync(userId));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verifyEmailResponse.StatusCode);
        Assert.NotNull(userDb);
        Assert.True(userDb.IsEmailVerified);
        Assert.Equal(user.Nick, userDb.Nick);
        Assert.Equal(user.Login, userDb.Login);
        Assert.Equal(user.Email, userDb.Email);
        //Password is hashed
        Assert.NotEqual(user.Password, userDb.Password);
    }


    [Fact]
    public async Task LoginExistingUser_ShouldGiveTokens()
    {
        var client = factory.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        await AuthHelper.FullRegisterFlowAsync(factory, client, user);

        var loginResponse = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));

        CookieContainer cookies = new CookieContainer();

        AuthHelper.SetCookies(cookies, loginResponse, client);

        var accessToken = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "access_token");
        var refreshToken = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "refresh_token");

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(accessToken);
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(accessToken.Value);
        Assert.NotEmpty(refreshToken.Value);
    }


    [Fact]
    public async Task LoginUserWrongPassword_Should401()
    {
        var client = factory.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        await AuthHelper.FullRegisterFlowAsync(factory, client, user);

        var response = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, "wrongPass"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshWithStaleToken_Should401()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        await AuthHelper.FullRegisterFlowAsync(factory, client, user);

        var loginResponse = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));

        CookieContainer cookies = new CookieContainer();

        AuthHelper.SetCookies(cookies, loginResponse, client);

        var accessToken1 = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "access_token");
        var refreshToken1 = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "refresh_token");

        var request1 = new HttpRequestMessage(HttpMethod.Post, AuthHelper.RefreshUri);
        request1.Headers.Add("Cookie", $"refresh_token={refreshToken1}");
        var refreshResponse = await client.SendAsync(request1);

        var request2 = new HttpRequestMessage(HttpMethod.Post, AuthHelper.RefreshUri);
        request2.Headers.Add("Cookie", $"refresh_token={refreshToken1}");
        var refreshReponseWithStaleToken = await client.SendAsync(request2);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshReponseWithStaleToken.StatusCode);
    }

    [Fact]
    public async Task RefreshTokensWhenAccessToExpired_ShouldGiveNew()
    {
        var client = factory.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        await AuthHelper.FullRegisterFlowAsync(factory, client, user);

        var loginResponse = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));

        CookieContainer cookies = new CookieContainer();

        AuthHelper.SetCookies(cookies, loginResponse, client);

        var accessToken1 = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "access_token");
        var refreshToken1 = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "refresh_token");

        var refreshResponse = await client.PostAsJsonAsync(AuthHelper.RefreshUri, new { });

        AuthHelper.SetCookies(cookies, refreshResponse, client);

        var accessToken2 = cookies.GetAllCookies().LastOrDefault(c => c.Name == "access_token");
        var refreshToken2 = cookies.GetAllCookies().LastOrDefault(c => c.Name == "refresh_token");

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task TryAccessAuthorizedEndpoint_ShouldBeDenied()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/lobbies", new CreateLobbyRequest("test", false));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}