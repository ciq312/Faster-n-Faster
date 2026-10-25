using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Presentation.Web.Auth.LoginUser;
using FasterNFaster.Api.Presentation.Web.Auth.RegisterUser;
using FasterNFaster.Api.Presentation.Web.Auth.VerifyEmail;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Auth.RegisterUser;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FasterNFaster.IntegrationTests;

public static class AuthHelper
{
    public static Task<HttpResponseMessage> Register(HttpClient client) =>
        client.PostAsJsonAsync(RegisterUri, NewPlayer());

    public static async Task<Guid> FullRegisterFlowAsync(
           WebApplicationFactory<Program> app, HttpClient client, RegisterUserRequest request)
    {
        var response = await client.PostAsJsonAsync(RegisterUri, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RegisterUserResult>();
        var userId = result!.UserId;

        var confirmToken = await app.ExecuteScopedAsync<IConfirmTokenRepository, Token?>(repo =>
            repo.GetLatestForUserAsync(userId, TokenType.EmailVerification));

        var verifyResponse = await client.PostAsJsonAsync(VerifyEmailUri, new VerifyEmailRequest(confirmToken!.Value));
        verifyResponse.EnsureSuccessStatusCode();

        return userId;
    }

    public static RegisterUserRequest NewPlayer()
    {
        var id = Guid.NewGuid().ToString("N")[..5];
        return new RegisterUserRequest($"Nick{id}", $"user{id}", $"{id}@example.com", "TestPassword123!");
    }

    public static async Task<LoginUserResponse> RegisterAndLoginAsync(
        WebApplicationFactory<Program> app, HttpClient client, RegisterUserRequest request)
    {
        await FullRegisterFlowAsync(app, client, request);

        var loginResponse = await client.PostAsJsonAsync(LoginUri, new LoginUserRequest(request.Login, request.Password));
        loginResponse.EnsureSuccessStatusCode();
        return await loginResponse.Content.ReadFromJsonAsync<LoginUserResponse>()
            ?? throw new InvalidOperationException("can't parse login result");
    }

    public static void SetCookies(CookieContainer cookies, HttpResponseMessage loginResponse, HttpClient client)
    {
        if (!loginResponse.Headers.TryGetValues("Set-Cookie", out var headerCookies)) throw new NotFoundException("cookies were not found");

        foreach (var cookie in headerCookies)
        {
            cookies.SetCookies(client.BaseAddress!, cookie);
        }
    }

    public static string RegisterUri => "api/auth/register";
    public static string LoginUri => "api/auth/login";
    public static string VerifyEmailUri => "api/auth/verify-email";
    public static string RefreshUri => "api/auth/refresh";
    public static string LogoutUri => "api/auth/logout";
    public static string GuestUri => "api/auth/guest";

}
