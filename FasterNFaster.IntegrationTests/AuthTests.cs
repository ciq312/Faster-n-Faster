using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.Presentation.Web.Lobbies.CreateLobby;
using FasterNFaster.Api.Presentation.Web.Users.LoginUser;
using FasterNFaster.Api.Presentation.Web.Users.RegisterUser;
using FasterNFaster.Api.Presentation.Web.Users.VerifyEmail;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Users.RegisterUser;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FasterNFaster.IntegrationTests;

public class AuthTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task FullRegisterFlow_ShouldPersistUser()
    {
        var client = App.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        var registerResponse = await client.PostAsJsonAsync(AuthHelper.RegisterUri, user);

        var result = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResult>();
        var userId = result!.UserId;

        var confirmEmailToken = await App.ExecuteScopedAsync<IConfirmTokenRepository, Token?>(repo =>
        repo.GetLatestForUserAsync(
            userId,
            TokenType.EmailVerification));

        var verifyEmailResponse = await client.PostAsJsonAsync(
            AuthHelper.VerifyEmailUri,
            new VerifyEmailRequest(confirmEmailToken!.Value));

        var userDb = await App.ExecuteScopedAsync<IUserRepository, User?>(
            repo => repo.GetByIdAsync(userId));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verifyEmailResponse.StatusCode);
        Assert.NotNull(userDb);
        Assert.True(userDb.IsEmailVerified);
        Assert.Equal(user.Nick, userDb.Nick);
        Assert.Equal(User.NormalizeLogin(user.Login), userDb.Login);
        Assert.Equal(user.Email, userDb.Email);
        //Password is hashed
        Assert.NotEqual(user.Password, userDb.Password);
    }

    [Fact]
    public async Task LoginUserWrongPassword_Returns401WithErrorAndNoCode()
    {
        var client = App.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        await AuthHelper.FullRegisterFlowAsync(App, client, user);

        var response = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, "wrongPass"));

        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(body!.RootElement.GetProperty("error").GetString()));
        Assert.False(body.RootElement.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task LoginUnverifiedUser_Returns403WithCodeAndEmail()
    {
        var client = App.CreateClient();

        var user = new RegisterUserRequest("unverified", "unverified", "unverified@example.com", "testPassword");
        var registerResponse = await client.PostAsJsonAsync(AuthHelper.RegisterUri, user);
        registerResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));

        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("EMAIL_NOT_VERIFIED", body!.RootElement.GetProperty("code").GetString());
        Assert.Equal("unverified@example.com", body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task RefreshWithAlreadyRotatedToken_Returns401()
    {
        var (client, refreshToken) = await LoginWithManualCookiesAsync();

        var firstRefresh = await PostWithRefreshCookieAsync(client, AuthHelper.RefreshUri, refreshToken);
        var replayedRefresh = await PostWithRefreshCookieAsync(client, AuthHelper.RefreshUri, refreshToken);

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, replayedRefresh.StatusCode);
    }

    [Fact]
    public async Task RefreshTokensWhenAccessToExpired_ShouldGiveNew()
    {
        var client = App.CreateClient();

        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");

        await AuthHelper.FullRegisterFlowAsync(App, client, user);

        var loginResponse = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));

        var cookies = new CookieContainer();

        AuthHelper.SetCookies(cookies, loginResponse, client);

        var refreshToken1 = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "refresh_token");

        var refreshResponse = await client.PostAsJsonAsync(AuthHelper.RefreshUri, new { });

        AuthHelper.SetCookies(cookies, refreshResponse, client);

        var refreshToken2 = cookies.GetAllCookies().FirstOrDefault(c => c.Name == "refresh_token");

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotEqual(refreshToken1!.Value, refreshToken2!.Value);

        var meResponse = await client.GetAsync("api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task TryAccessAuthorizedEndpoint_ShouldBeDenied()
    {
        var client = App.CreateClient();

        var response = await client.PostAsJsonAsync("/api/lobbies", new CreateLobbyRequest("test", false));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutWithExpiredAccessToken_RevokesRefreshToken()
    {
        var (client, refreshToken) = await LoginWithManualCookiesAsync();

        var logoutResponse = await PostWithRefreshCookieAsync(client, AuthHelper.LogoutUri, refreshToken);
        var refreshResponse = await PostWithRefreshCookieAsync(client, AuthHelper.RefreshUri, refreshToken);

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    private async Task<(HttpClient Client, string RefreshToken)> LoginWithManualCookiesAsync()
    {
        var client = App.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var user = new RegisterUserRequest("testUser", "testLogin", "test@gmail.com", "testPassword");
        await AuthHelper.FullRegisterFlowAsync(App, client, user);

        var loginResponse = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));
        return (client, RefreshCookieValue(loginResponse));
    }

    private static Task<HttpResponseMessage> PostWithRefreshCookieAsync(HttpClient client, string uri, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        return client.SendAsync(request);
    }

    private static string RefreshCookieValue(HttpResponseMessage response) => CookieValue(response, "refresh_token");

    private static string CookieValue(HttpResponseMessage response, string name)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(h => h.StartsWith($"{name}="));
        return setCookie.Split(';')[0][(name.Length + 1)..];
    }
}
