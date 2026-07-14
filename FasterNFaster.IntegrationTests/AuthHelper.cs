using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Users.RegisterUsers;
using FasterNFaster.Api.Web.Users.RegisterUser;
using FasterNFaster.Api.Web.Users.RegisterUser.EndPoints;
using FasterNFaster.IntegrationTests;

public static class AuthHelper
{
    public static Task<HttpResponseMessage> Register(HttpClient client)
    {
        var id = Guid.NewGuid().ToString("N")[..5];
        return client.PostAsJsonAsync(RegisterUri, new RegisterUserRequest($"Test{id}", $"login{id}", $"{id}@example.com", "TestPassword123!"));
    }

    public static async Task<Guid> FullRegisterFlowAsync(
           TestApplicationFactory<Program> factory, HttpClient client, RegisterUserRequest request)
    {
        var response = await client.PostAsJsonAsync(RegisterUri, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RegisterUserResult>();
        var userId = result!.UserId;

        var confirmToken = await factory.ExecuteScopedAsync<IConfirmTokenRepository, Token?>(repo =>
            repo.GetLatestForUserAsync(userId, TokenType.EmailVerification));

        var verifyResponse = await client.PostAsJsonAsync(VerifyEmailUri, new VerifyEmailRequest(confirmToken!.Value));
        verifyResponse.EnsureSuccessStatusCode();

        return userId;
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

}