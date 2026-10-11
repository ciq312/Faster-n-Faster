using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Services.Races;
using Microsoft.Extensions.Hosting;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class RaceTickService(
    IRaceTickRegistry registry,
    ILobbyAccess lobbies,
    IBroadcaster broadcaster,
    IRaceAccess races,
    RaceStateConflator conflator,
    TimeProvider timeProvider,
    ILogger<RaceTickService> logger) : BackgroundService
{
    private const int TickIntervalMs = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickIntervalMs), timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await Tick();
    }

    public async Task Tick()
    {
        var racingLobbies = registry.GetRacingLobbies();
        await Task.WhenAll(racingLobbies.Select(TickLobby));

        if (conflator.TrackedLobbies != racingLobbies.Count)
            conflator.Prune(racingLobbies.Select(entry => entry.LobbyId).ToHashSet());
    }

    private async Task TickLobby(RacingLobbyEntry entry)
    {
        try
        {
            if (!lobbies.Exists(entry.LobbyId))
            {
                registry.DeregisterLobby(entry.LobbyId);
                return;
            }

            if (entry.Phase == RacePhase.Countdown)
                await HandleCountdown(entry);
            else
                await HandleRacing(entry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tick failed for lobby {LobbyId}", entry.LobbyId);
        }
    }

    private async Task HandleCountdown(RacingLobbyEntry entry)
    {
        var elapsed = timeProvider.GetUtcNow().UtcDateTime - entry.RegisteredAt;

        if (elapsed >= RaceCountdown.Duration + RaceCountdown.StartDelay)
        {
            if (!await races.TryMutate(entry.LobbyId, r => r.Start())) return;

            await broadcaster.Broadcast(Audience.Lobby(entry.LobbyId), GameEvents.RaceStarted);
            registry.TransitionToRacing(entry.LobbyId);
        }
    }

    private async Task HandleRacing(RacingLobbyEntry entry)
    {
        if (!await races.TryMutate(entry.LobbyId, r => r.RetryPendingClaims())) return;

        var snapshot = await races.GetSnapshotOrDefault(entry.LobbyId);
        if (snapshot is null) return;

        var connectedPlayerIds = await lobbies.TryRead(entry.LobbyId, l => l.GetPlayerIds());
        if (connectedPlayerIds is null) return;

        conflator.Publish(entry.LobbyId, connectedPlayerIds, snapshot);
    }
}
