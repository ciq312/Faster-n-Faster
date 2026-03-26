using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure.Hubs;
using FasterNFaster.Api.UseCases.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.UseCases.Services;

public class RaceTickService : BackgroundService
{
    private readonly IRaceTickRegistry _registry;
    private readonly ILobbyStore _lobbyStore;
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<RaceTickService> _logger;

    private const int TickIntervalMs = 50;
    private const float CountdownSeconds = 3.5f;

    public RaceTickService(
        IRaceTickRegistry registry,
        ILobbyStore lobbyStore,
        IHubContext<GameHub> hub,
        ILogger<RaceTickService> logger)
    {
        _registry = registry;
        _lobbyStore = lobbyStore;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var lobbies = _registry.GetRacingLobbies();

            foreach (var entry in lobbies)
            {
                try
                {
                    var lobby = _lobbyStore.Get(entry.LobbyId);
                    if (lobby == null)
                    {
                        _registry.DeregisterLobby(entry.LobbyId);
                        continue;
                    }

                    var group = _hub.Clients.Group($"lobby-{entry.LobbyId}");

                    if (entry.Phase == RacePhase.Countdown)
                        await HandleCountdown(entry, lobby, group);
                    else
                        await HandleRacing(entry, lobby, group);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tick failed for lobby {LobbyId}", entry.LobbyId);
                }
            }
        }
    }

    private async Task HandleCountdown(RacingLobbyEntry entry, Lobby lobby, IClientProxy group)
    {
        var elapsed = (DateTime.UtcNow - entry.RegisteredAt).TotalSeconds;

        if (elapsed >= CountdownSeconds)
        {
            var race = lobby.GetRace();
            await group.SendAsync("RaceStarted", new { words = race.Words });
            _registry.TransitionToRacing(entry.LobbyId);
            return;
        }

    }

    private async Task HandleRacing(RacingLobbyEntry entry, Lobby lobby, IClientProxy group)
    {
        var race = lobby.GetRace();
        var snapshot = race.GetSnapshot();

        var connectedPlayerIds = lobby.Players
            .Where(p => p.IsConnected)
            .Select(p => p.User.Id)
            .ToHashSet();

        var players = snapshot
            .Where(s => connectedPlayerIds.Contains(s.PlayerId))
            .Select(s => new { playerId = s.PlayerId, index = s.Index, wpm = s.Wpm });

        if (!players.Any())
        {
            _registry.DeregisterLobby(entry.LobbyId);
            return;
        }

        await group.SendAsync("RaceState", new { players });
    }
}
