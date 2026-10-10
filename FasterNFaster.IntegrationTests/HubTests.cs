
using System.Net;
using System.Net.Http.Json;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.AntiCheat;
using FasterNFaster.Api.Web.Lobbies.CreateLobby;
using FasterNFaster.Api.Web.Users.LoginUser;
using FasterNFaster.Api.Web.Users.RegisterUser;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
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
    public async Task SuspendedUserConnect_ShouldAbort()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        await using var hub = BuildHubConnection(client, cookies);

        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Closed += _ =>
        {
            connectionClosed.TrySetResult();
            return Task.CompletedTask;
        };

        await app.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(loginResult.UserId, "test suspension", DateTime.UtcNow.AddHours(1)));

        await hub.StartAsync();

        await connectionClosed.Task.WaitAsync(EventTimeout);
        Assert.Equal(HubConnectionState.Disconnected, hub.State);
    }

    [Fact]
    public async Task ExpiredSuspensionUserConnect_ShouldSucceed()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        await using var hub = BuildHubConnection(client, cookies);

        await app.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(loginResult.UserId, "test suspension", DateTime.UtcNow.AddSeconds(-1)));

        await hub.StartAsync();
        await hub.InvokeAsync<long>("Ping", 0);

        Assert.Equal(HubConnectionState.Connected, hub.State);
    }

    [Fact]
    public async Task SuspendedWhileConnected_ClosesConnectionOnNextInvocation()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        await using var hub = BuildHubConnection(client, cookies);

        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Closed += _ =>
        {
            connectionClosed.TrySetResult();
            return Task.CompletedTask;
        };

        await hub.StartAsync();
        await hub.InvokeAsync<long>("Ping", 0);

        var expiresAt = DateTime.UtcNow.AddHours(1);
        await app.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(loginResult.UserId, "test suspension", expiresAt));
        await app.ExecuteScopedAsync<IBroadcaster>(broadcaster =>
            broadcaster.Broadcast(Audience.Player(loginResult.UserId), GameEvents.Suspended, new SuspendedDTO(expiresAt)));

        // HubSuspensionFilter only catches the ban on this connection's next call, and
        // Abort() can race with delivering this call's own response, so the invoke itself
        // may legitimately fault - only the resulting close matters here.
        try
        {
            await hub.InvokeAsync<long>("Ping", 0);
        }
        catch (Exception)
        {
        }

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

    [Fact]
    public async Task NonHostStartRace_ShouldThrowHubException()
    {
        var (hostClient, hostCookies, _) = await RegisterAndLoginAsync();
        var createResponse = await hostClient.PostAsJsonAsync("/api/lobbies", new CreateLobbyRequest("test", false));
        var lobby = await createResponse.Content.ReadFromJsonAsync<CreateLobbyResult>()
            ?? throw new InvalidOperationException("can't parse create lobby result");

        await using var hostHub = BuildHubConnection(hostClient, hostCookies);
        await hostHub.StartAsync();
        await hostHub.InvokeAsync("ConnectToLobby", lobby.LobbyId, lobby.inviteCode);

        var (guestClient, guestCookies, _) = await RegisterAndLoginAsync(NewUser("guest"));
        await using var guestHub = BuildHubConnection(guestClient, guestCookies);
        await guestHub.StartAsync();
        await guestHub.InvokeAsync("ConnectToLobby", lobby.LobbyId, lobby.inviteCode);

        var ex = await Assert.ThrowsAsync<HubException>(() => guestHub.InvokeAsync("StartRace"));

        Assert.EndsWith("Only the host can start the race", ex.Message);
    }

    private static RegisterUserRequest NewUser(string prefix = "test") => new(prefix, prefix, $"{prefix}@gmail.com", "testpass");

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
