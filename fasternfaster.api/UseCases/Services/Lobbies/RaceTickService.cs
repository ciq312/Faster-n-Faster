using FasternFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.UseCases.Services;

public class RaceTickService(
    IRaceTickRegistry registry,
    ILobbyStore lobbyStore,
    IHubContext<GameHub> hub,
    IRaceTransitionService raceTransitionService,
    IRaceService raceService) : BackgroundService
{
    private readonly IRaceService raceService = raceService;
    private readonly IRaceTickRegistry registry = registry;
    private readonly IRaceTransitionService raceTransitionService = raceTransitionService;
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IHubContext<GameHub> hub = hub;

    private const int TickIntervalMs = 100;
    private const float CountdownSeconds = 3.5f;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var lobbies = registry.GetRacingLobbies();

            foreach (var entry in lobbies)
            {
                try
                {
                    var lobby = lobbyStore.Get(entry.LobbyId);
                    if (lobby == null)
                    {
                        registry.DeregisterLobby(entry.LobbyId);
                        continue;
                    }

                    var group = hub.Clients.Group($"lobby-{entry.LobbyId}");

                    if (entry.Phase == RacePhase.Countdown)
                        await HandleCountdown(entry, lobby, group);
                    else
                        await HandleRacing(entry, lobby, group);
                }
                catch (Exception ex)
                {
                    Log.Information(ex, "Tick failed for lobby {LobbyId}", entry.LobbyId);
                }
            }
        }
    }

    private async Task HandleCountdown(RacingLobbyEntry entry, Lobby lobby, IClientProxy group)
    {
        var elapsed = (DateTime.UtcNow - entry.RegisteredAt).TotalSeconds;

        if (elapsed >= CountdownSeconds)
        {
            await raceTransitionService.StartRaceInternal(entry.LobbyId);

            await group.SendAsync("RaceStarted");
#if DEBUG
            Log.Information("Race started in lobby {LobbyId}", entry.LobbyId);
#endif
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

        await group.SendAsync("RaceState", players);
    }
}
