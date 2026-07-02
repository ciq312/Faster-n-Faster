using FasternFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class RaceTickService(
    IRaceTickRegistry registry,
    ILobbyStore lobbyStore,
    IRaceBroadcaster broadcaster,
    IRaceTransitionService raceTransitionService,
    IRaceService raceService,
    RaceStateConflator conflator,
    ILogger<RaceTickService> logger) : BackgroundService
{
    private const int TickIntervalMs = 200;
    private const float CountdownSeconds = 3.5f;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var lobbies = registry.GetRacingLobbies();
            await Task.WhenAll(lobbies.Select(TickLobby));

            if (conflator.TrackedLobbies != lobbies.Count)
                conflator.Prune(lobbies.Select(entry => entry.LobbyId).ToHashSet());
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

            if (entry.Phase == RacePhase.Countdown)
                await HandleCountdown(entry);
            else
                await HandleRacing(entry, lobby);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tick failed for lobby {LobbyId}", entry.LobbyId);
        }
    }

    private async Task HandleCountdown(RacingLobbyEntry entry)
    {
        var elapsed = (DateTime.UtcNow - entry.RegisteredAt).TotalSeconds;

        if (elapsed >= CountdownSeconds)
        {
            await raceTransitionService.StartRaceInternal(entry.LobbyId);
            await broadcaster.BroadcastRaceStarted(entry.LobbyId);
            registry.TransitionToRacing(entry.LobbyId);
        }
    }

    private async Task HandleRacing(RacingLobbyEntry entry, Lobby lobby)
    {
        var snapshot = await raceService.GetSnapshot(entry.LobbyId);

        var connectedPlayerIds = lobby.Players
            .Where(p => p.IsConnected)
            .Select(p => p.User.Id)
            .ToList();

        var visibleSnapshot = snapshot
            .Where(s => connectedPlayerIds.Contains(s.PlayerId))
            .ToList();

        conflator.Publish(entry.LobbyId, connectedPlayerIds, visibleSnapshot);
    }
}
