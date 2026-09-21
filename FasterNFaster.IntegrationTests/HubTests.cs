
using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Web.Users.LoginUser;
using FasterNFaster.Api.Web.Users.RegisterUser;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace FasterNFaster.IntegrationTests;

public class HubTests(NoRateLimitApplicationFactory<Program> fixture) : IClassFixture<NoRateLimitApplicationFactory<Program>>, IAsyncLifetime
{
    private WebApplicationFactory<Program> app = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        app = fixture.CreateApp();
    }

    public async Task DisposeAsync() => await app.DisposeAsync();

    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task BannedUserConnect_ShouldAbort()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        await using var hub = BuildHubConnection(client, cookies);

        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Closed += _ =>
        {
            connectionClosed.TrySetResult();
            return Task.CompletedTask;
        };

        await app.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(loginResult.UserId, "no reason"));

        await hub.StartAsync();

        await connectionClosed.Task.WaitAsync(EventTimeout);
        Assert.Equal(HubConnectionState.Disconnected, hub.State);
    }

    [Fact]
    public async Task UnauthenticatedUserConnection_ShouldReject()
    {
        await using var hub = BuildHubConnection(app.CreateClient(), cookies: null);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task AnotherSession_ShouldSendEvent()
    {
        var user = NewUser();
        var (client, cookies, _) = await RegisterAndLoginAsync(user);
        await using var hub = BuildHubConnection(client, cookies);

        var anotherSessionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On("AnotherSessionStarted", () => anotherSessionStarted.TrySetResult());

        await hub.StartAsync();
        await hub.InvokeAsync<long>("Ping", 0);

        var (anotherClient, anotherCookies, _) = await LoginAsync(app.CreateClient(), user);
        await using var anotherHub = BuildHubConnection(anotherClient, anotherCookies);
        await anotherHub.StartAsync();

        await anotherSessionStarted.Task.WaitAsync(EventTimeout);
    }

    private static RegisterUserRequest NewUser() => new("test", "test", "test@gmail.com", "testpass");

    private async Task<(HttpClient Client, CookieContainer Cookies, LoginUserResult LoginResult)> RegisterAndLoginAsync(RegisterUserRequest? user = null)
    {
        user ??= NewUser();
        var client = app.CreateClient();

        await AuthHelper.FullRegisterFlowAsync(app, client, user);

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

                options.HttpMessageHandlerFactory = _ => app.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }
}
