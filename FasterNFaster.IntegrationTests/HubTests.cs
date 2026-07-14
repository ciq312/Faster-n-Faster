
using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Web.Users.LoginUser;
using FasterNFaster.Api.Web.Users.RegisterUser;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace FasterNFaster.IntegrationTests;

public class HubTests(NoRateLimitApplicationFactory<Program> factory) : IClassFixture<NoRateLimitApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly NoRateLimitApplicationFactory<Program> factory = factory;

    public Task InitializeAsync() => factory.ResetDb();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BannedUserConnect_ShouldAbort()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        var hub = BuildHubConnection(client, cookies);

        await factory.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(loginResult.UserId, "no reason"));

        await hub.StartAsync();

        //for filter to abort connection
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal(HubConnectionState.Disconnected, hub.State);
    }

    [Fact]
    public async Task UnauthenticatedUserConnection_ShouldReject()
    {
        var hub = BuildHubConnection(factory.CreateClient(), cookies: null);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task AnotherSession_ShouldSendEvent()
    {
        bool isAnotherSessionStarted = false;

        var user = NewUser();
        var (client, cookies, _) = await RegisterAndLoginAsync(user);
        var hub = BuildHubConnection(client, cookies);

        hub.On("AnotherSessionStarted", () => isAnotherSessionStarted = true);

        await hub.StartAsync();

        var (_, anotherCookies, _) = await LoginAsync(factory.CreateClient(), user);
        var anotherHub = BuildHubConnection(factory.CreateClient(), anotherCookies);

        await Task.Delay(TimeSpan.FromSeconds(1));

        await anotherHub.StartAsync();

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.True(isAnotherSessionStarted);
    }

    private static RegisterUserRequest NewUser() => new("test", "test", "test@gmail.com", "testpass");

    private async Task<(HttpClient Client, CookieContainer Cookies, LoginUserResult LoginResult)> RegisterAndLoginAsync(RegisterUserRequest? user = null)
    {
        user ??= NewUser();
        var client = factory.CreateClient();

        await AuthHelper.FullRegisterFlowAsync(factory, client, user);

        return await LoginAsync(client, user);
    }

    private static async Task<(HttpClient Client, CookieContainer Cookies, LoginUserResult LoginResult)> LoginAsync(HttpClient client, RegisterUserRequest user)
    {
        var loginResponse = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginUserResult>()
            ?? throw new InvalidOperationException("can't parse login result");

        var cookies = new CookieContainer();
        AuthHelper.SetCookies(cookies, loginResponse, client);

        return (client, cookies, loginResult);
    }

    private HubConnection BuildHubConnection(HttpClient client, CookieContainer? cookies)
    {
        return new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress}gameHub", options =>
            {
                if (cookies is not null)
                    options.Headers["Cookie"] = cookies.GetCookieHeader(client.BaseAddress!);

                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }
}
