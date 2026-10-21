
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.Presentation.Web.AntiCheat;
using FasterNFaster.Api.Presentation.Web.Auth.LoginUser;
using FasterNFaster.Api.Presentation.Web.Auth.RegisterUser;
using FasterNFaster.Api.Presentation.Web.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.AntiCheat.Sanctions;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Realtime.RaceFinished;
using FasterNFaster.Api.UseCases.Realtime.RaceStarting;
using FasterNFaster.Api.UseCases.Users.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FasterNFaster.IntegrationTests;

public class HubTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    // FullRace sends each whole passage in one update; lift the typing budget so anti-cheat doesn't clamp it.
    // One race violation then suspends the player, so a single bad update drives the real sanction path.
    protected override void ConfigureApp(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<AntiCheatOptions>(options => options.BudgetSlack = 100_000);
            services.PostConfigure<AntiCheatSanctionOptions>(options =>
            {
                options.RaceViolationThreshold = 1;
                options.SuspensionThreshold = 1;
            });
            services.AddSingleton<DbCommandCounter>();
            services.ConfigureDbContext<AppDbContext>((sp, options) =>
                options.AddInterceptors(sp.GetRequiredService<DbCommandCounter>()));
        });

    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RaceStartTimeout = RaceCountdown.Duration + RaceCountdown.StartDelay + EventTimeout;
    private static readonly TimeSpan StatisticsPollInterval = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task BannedUserConnect_ShouldAbort()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        await using var hub = BuildHubConnection(client, cookies);

        var connectionClosed = CloseSignal(hub);

        await App.ExecuteScopedAsync<IBanRepository>(repo => repo.BanAsync(loginResult.UserId, "no reason"));

        await hub.StartAsync();

        await connectionClosed.WaitAsync(EventTimeout);
        Assert.Equal(HubConnectionState.Disconnected, hub.State);
    }

    [Fact]
    public async Task SuspendedUserConnect_ShouldAbort()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        await using var hub = BuildHubConnection(client, cookies);
        var connectionClosed = CloseSignal(hub);

        await App.ExecuteScopedAsync<IBanRepository>(repo =>
            repo.SuspendAsync(loginResult.UserId, "test suspension", DateTime.UtcNow.AddHours(1)));

        await hub.StartAsync();

        await connectionClosed.WaitAsync(EventTimeout);
        Assert.Equal(HubConnectionState.Disconnected, hub.State);
    }

    [Fact]
    public async Task RaceViolation_SuspendsAndClosesConnectionWithoutFurtherCalls()
    {
        var (hub, userId, _) = await StartSoloRaceAsync();
        await using var _ = hub;
        var connectionClosed = CloseSignal(hub);
        const int indexBeyondTyped = 5;

        // Abort() can race with delivering this call's own response, so the invoke itself
        // may legitimately fault - only the resulting close and suspension matter here.
        await Record.ExceptionAsync(() => hub.InvokeAsync("UpdateRaceState", indexBeyondTyped, 0, ""));

        await connectionClosed.WaitAsync(EventTimeout);
        Assert.Equal(HubConnectionState.Disconnected, hub.State);
        Assert.True(await App.ExecuteScopedAsync<IBanRepository, bool>(repo => repo.IsBannedAsync(userId)));
    }

    [Fact]
    public async Task UnauthenticatedUserConnection_ShouldReject()
    {
        await using var hub = BuildHubConnection(App.CreateClient(), cookies: null);

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

        var (anotherClient, anotherCookies, _) = await LoginAsync(App.CreateClient(), user);
        await using var anotherHub = BuildHubConnection(anotherClient, anotherCookies);
        await anotherHub.StartAsync();

        await anotherSessionStarted.Task.WaitAsync(EventTimeout);
    }

    [Fact]
    public async Task NonHostStartRace_ShouldThrowHubException()
    {
        var (hostClient, hostCookies, _) = await RegisterAndLoginAsync();
        var lobby = await CreateLobbyAsync(hostClient);
        await using var hostHub = await JoinLobbyAsync(hostClient, hostCookies, lobby);

        var (guestClient, guestCookies, _) = await RegisterAndLoginAsync(NewUser("guest"));
        await using var guestHub = await JoinLobbyAsync(guestClient, guestCookies, lobby);

        var ex = await Assert.ThrowsAsync<HubException>(() => guestHub.InvokeAsync("StartRace"));

        Assert.EndsWith("Only the host can start the race", ex.Message);
    }

    [Fact]
    public async Task FullRace_TwoPlayers_FinishInOrderAndPersistStatistics()
    {
        var (hostClient, hostCookies, host) = await RegisterAndLoginAsync();
        var (guestClient, guestCookies, guest) = await RegisterAndLoginAsync(NewUser("guest"));
        var lobby = await CreateLobbyAsync(hostClient);
        await using var hostHub = await JoinLobbyAsync(hostClient, hostCookies, lobby);
        await using var guestHub = await JoinLobbyAsync(guestClient, guestCookies, lobby);

        var raceStarting = NextEventAsync<RaceStartingDTO>(guestHub, nameof(IGameClient.RaceStarting));
        var hostRaceStarted = NextEventAsync(hostHub, nameof(IGameClient.RaceStarted));
        var guestRaceStarted = NextEventAsync(guestHub, nameof(IGameClient.RaceStarted));
        var hostRaceEnded = NextEventAsync<RaceEndedDTO>(hostHub, nameof(IGameClient.RaceEnded));
        var guestRaceEnded = NextEventAsync<RaceEndedDTO>(guestHub, nameof(IGameClient.RaceEnded));

        await hostHub.InvokeAsync("StartRace");

        var starting = await raceStarting.WaitAsync(EventTimeout);
        Assert.Equal(RaceCountdown.Duration.TotalSeconds, starting.CountdownSeconds);
        await Task.WhenAll(hostRaceStarted, guestRaceStarted).WaitAsync(RaceStartTimeout);

        var (passage, wordCount) = await App.Services
            .GetRequiredService<IRaceStore>()
            .Read(lobby.LobbyId, r => (r.Passage, r.WordCount));
        await hostHub.InvokeAsync("UpdateRaceState", passage.Length - 1, 0, passage);
        await guestHub.InvokeAsync("UpdateRaceState", passage.Length - 1, 0, passage);

        var ended = await hostRaceEnded.WaitAsync(EventTimeout);
        await guestRaceEnded.WaitAsync(EventTimeout);
        var hostResult = Assert.Single(ended.Results, r => r.PlayerId == host.UserId);
        var guestResult = Assert.Single(ended.Results, r => r.PlayerId == guest.UserId);
        Assert.Equal(1, hostResult.FinishPosition);
        Assert.Equal(2, guestResult.FinishPosition);
        Assert.Equal(100, hostResult.Accuracy);
        Assert.Equal(100, guestResult.Accuracy);

        Assert.DoesNotContain(
            App.Services.GetRequiredService<IRaceTickRegistry>().GetRacingLobbies(),
            entry => entry.LobbyId == lobby.LobbyId);
        Assert.False(await App.Services.GetRequiredService<ILobbyStore>().Read(lobby.LobbyId, l => l.IsSessionActive));

        var hostStatistics = await WaitForStatisticsAsync(host.UserId);
        Assert.Equal(1, hostStatistics.Wins);
        Assert.Equal(1, hostStatistics.RacesTyped);
        Assert.Equal(wordCount, hostStatistics.WordsTyped);
        Assert.Equal(100, hostStatistics.BestAccuracy);

        var guestStatistics = await WaitForStatisticsAsync(guest.UserId);
        Assert.Equal(0, guestStatistics.Wins);
        Assert.Equal(1, guestStatistics.RacesTyped);
    }

    [Fact]
    public async Task StartRace_SendsLobbyStateWithRaceStartingOnly()
    {
        var (client, cookies, _) = await RegisterAndLoginAsync();
        var lobby = await CreateLobbyAsync(client);
        await using var hub = await JoinLobbyAsync(client, cookies, lobby);
        var lobbyState = NextEventAsync<JsonElement>(hub, nameof(IGameClient.LobbyState));

        await hub.InvokeAsync("StartRace");

        var state = await lobbyState.WaitAsync(EventTimeout);
        Assert.True(state.GetProperty("isRaceStarting").GetBoolean());
        Assert.False(state.GetProperty("isRaceStarted").GetBoolean());
    }

    [Fact]
    public async Task RefreshLobbyMidRace_SendsLobbyStateWithRaceStartedOnly()
    {
        var (hub, _, _) = await StartSoloRaceAsync();
        await using var _ = hub;
        var lobbyState = NextEventAsync<JsonElement>(hub, nameof(IGameClient.LobbyState));

        await hub.InvokeAsync("RefreshLobby");

        var state = await lobbyState.WaitAsync(EventTimeout);
        Assert.False(state.GetProperty("isRaceStarting").GetBoolean());
        Assert.True(state.GetProperty("isRaceStarted").GetBoolean());
    }

    private static Task CloseSignal(HubConnection hub)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };
        return closed.Task;
    }

    private async Task<(HubConnection Hub, Guid UserId, Guid LobbyId)> StartSoloRaceAsync()
    {
        var (client, cookies, loginResult) = await RegisterAndLoginAsync();
        var lobby = await CreateLobbyAsync(client);
        var hub = await JoinLobbyAsync(client, cookies, lobby);

        var raceStarted = NextEventAsync(hub, nameof(IGameClient.RaceStarted));
        await hub.InvokeAsync("StartRace");
        await raceStarted.WaitAsync(RaceStartTimeout);

        return (hub, loginResult.UserId, lobby.LobbyId);
    }

    private static async Task TypeWithoutFinishingAsync(HubConnection hub, string passage)
    {
        const int maxUpdates = 20;
        var updates = Math.Min(maxUpdates, passage.Length - 1);
        for (var i = 0; i < updates; i++)
            await hub.InvokeAsync("UpdateRaceState", i, 0, passage[..(i + 1)]);
    }

    private static Task<T> NextEventAsync<T>(HubConnection hub, string eventName)
    {
        var received = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<T>(eventName, payload => received.TrySetResult(payload));
        return received.Task;
    }

    private static Task NextEventAsync(HubConnection hub, string eventName)
    {
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On(eventName, () => received.TrySetResult());
        return received.Task;
    }

    private async Task<PlayerStatistics> WaitForStatisticsAsync(Guid userId)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < EventTimeout)
        {
            var statistics = await App.ExecuteScopedAsync<IStatisticsRepository, PlayerStatistics?>(repo => repo.FindAsync(userId));
            if (statistics is not null) return statistics;

            await Task.Delay(StatisticsPollInterval);
        }

        throw new TimeoutException($"Statistics for user {userId} were not saved within {EventTimeout}");
    }

    private static async Task<CreateLobbyResult> CreateLobbyAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/lobbies", new CreateLobbyRequest("test", false));

        return await response.Content.ReadFromJsonAsync<CreateLobbyResult>()
            ?? throw new InvalidOperationException("can't parse create lobby result");
    }

    private async Task<HubConnection> JoinLobbyAsync(HttpClient client, CookieContainer cookies, CreateLobbyResult lobby)
    {
        var hub = BuildHubConnection(client, cookies);
        await hub.StartAsync();
        await hub.InvokeAsync("ConnectToLobby", lobby.LobbyId, lobby.InviteCode);

        return hub;
    }

    private static RegisterUserRequest NewUser(string prefix = "test") => new(prefix, prefix, $"{prefix}@gmail.com", "testpass");

    private async Task<(HttpClient Client, CookieContainer Cookies, LoginUserResponse LoginResult)> RegisterAndLoginAsync(RegisterUserRequest? user = null)
    {
        user ??= NewUser();
        var client = App.CreateClient();

        await AuthHelper.FullRegisterFlowAsync(App, client, user);

        return await LoginAsync(client, user);
    }

    private static async Task<(HttpClient Client, CookieContainer Cookies, LoginUserResponse LoginResult)> LoginAsync(HttpClient client, RegisterUserRequest user)
    {
        var loginResponse = await client.PostAsJsonAsync(AuthHelper.LoginUri, new LoginUserRequest(user.Login, user.Password));

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginUserResponse>()
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

                options.HttpMessageHandlerFactory = _ => App.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }
}
