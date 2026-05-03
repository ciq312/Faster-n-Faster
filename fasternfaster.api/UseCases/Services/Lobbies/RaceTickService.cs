using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.UseCases.Services;

public class RaceTickService(
    IRaceTickRegistry registry,
    ILobbyStore lobbyStore,
    ILobbyService lobbyService,
    IHubContext<GameHub> hub,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly IRaceTickRegistry registry = registry;
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly ILobbyService lobbyService = lobbyService;
    private readonly IHubContext<GameHub> hub = hub;

    private const int TickIntervalMs = 50;
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
            await lobbyService.LaunchSession(entry.LobbyId);
            await group.SendAsync("RaceStarted");
#if DEBUG
            Log.Information("Race started in lobby {LobbyId}", entry.LobbyId);
#endif
            registry.TransitionToRacing(entry.LobbyId);
        }
    }

    private async Task HandleRacing(RacingLobbyEntry entry, Lobby lobby, IClientProxy group)
    {
        var snapshot = lobby.Race.GetSnapshot();

        var connectedPlayerIds = lobby.Players
            .Where(p => p.IsConnected)
            .Select(p => p.User.Id)
            .ToHashSet();

        var players = snapshot
            .Where(s => connectedPlayerIds.Contains(s.PlayerId))
            .ToList();

        var race = lobby.Race;

        if (race.IsRaceFinished)
        {
            var results = race.GetRaceStatics();
            registry.DeregisterLobby(entry.LobbyId);
            using var scope = scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
            await dispatcher.Dispatch(new RaceFinishedEvent(entry.LobbyId, results));
            return;
        }

        if (players.Count == 0)
        {
            registry.DeregisterLobby(entry.LobbyId);
            return;
        }

        await group.SendAsync("RaceState", players);
    }
}
