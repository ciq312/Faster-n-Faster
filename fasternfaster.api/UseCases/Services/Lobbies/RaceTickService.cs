using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using FasternFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Services;

public class RaceTickService(
    IRaceTickRegistry registry,
    ILobbyStore lobbyStore,
    IHubContext<GameHub> hub,
    IRaceTransitionService raceTransitionService,
    IRaceService raceService,
    ISessionService sessionService,
    ILogger<RaceTickService> logger) : BackgroundService
{
    private readonly IRaceService raceService = raceService;
    private readonly IRaceTickRegistry registry = registry;
    private readonly IRaceTransitionService raceTransitionService = raceTransitionService;
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IHubContext<GameHub> hub = hub;
    private readonly ISessionService sessionService = sessionService;
    private readonly ILogger<RaceTickService> logger = logger;

    private const int TickIntervalMs = 200;
    private const float CountdownSeconds = 3.5f;
    private const int PerClientSendTimeoutMs = 150;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var lobbies = registry.GetRacingLobbies();

            await Task.WhenAll(lobbies.Select(TickLobby));
        }
    }

    private async Task TickLobby(RacingLobbyEntry entry)
    {
        try
        {
            var lobby = lobbyStore.Get(entry.LobbyId);
            if (lobby == null)
            {
                registry.DeregisterLobby(entry.LobbyId);
                return;
            }

            var group = hub.Clients.Group($"lobby-{entry.LobbyId}");

            if (entry.Phase == RacePhase.Countdown)
                await HandleCountdown(entry, lobby, group);
            else
                await HandleRacing(entry, lobby, group);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tick failed for lobby {LobbyId}", entry.LobbyId);
        }
    }

    private async Task HandleCountdown(RacingLobbyEntry entry, Lobby lobby, IClientProxy group)
    {
        var elapsed = (DateTime.UtcNow - entry.RegisteredAt).TotalSeconds;

        if (elapsed >= CountdownSeconds)
        {
            await raceTransitionService.StartRaceInternal(entry.LobbyId);
            await group.SendAsync("RaceStarted");
            registry.TransitionToRacing(entry.LobbyId);
        }
    }

    private async Task HandleRacing(RacingLobbyEntry entry, Lobby lobby, IClientProxy group)
    {
        var snapshot = await raceService.GetSnapshot(entry.LobbyId);

        var connectedPlayerIds = lobby.Players
            .Where(p => p.IsConnected)
            .Select(p => p.User.Id)
            .ToHashSet();

        var players = snapshot
            .Where(s => connectedPlayerIds.Contains(s.PlayerId))
            .ToList();

        var sends = lobby.Players
            .Where(p => p.IsConnected)
            .Select(p => SendToPlayerWithTimeout(p.User.Id, "RaceState", players));

        await Task.WhenAll(sends);
    }

    private async Task SendToPlayerWithTimeout(Guid userId, string method, object payload)
    {
        var connectionId = sessionService.GetActiveSession(userId);
        if (connectionId is null) return;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(PerClientSendTimeoutMs));
        try
        {
            await hub.Clients.Client(connectionId).SendAsync(method, payload, cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Send to user {UserId} timed out", userId);
        }
    }
}
