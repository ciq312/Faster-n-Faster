using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.AntiCheat;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Handlers;

public class RaceViolationHandlerTests
{
    private const int RaceViolationThreshold = 3;
    private const int SuspensionThreshold = 3;

    [Fact]
    public async Task Handle_BelowRaceViolationThreshold_DoesNotWithdrawOrBroadcast()
    {
        var (handler, lobby, broadcaster, _, _, playerId, _) = await Build();

        await handler.Handle(Violation(lobby.LobbyId, playerId), CancellationToken.None);

        var race = await lobby.RaceAccess.GetSnapshotOrDefault(lobby.LobbyId);
        Assert.NotNull(race);
        Assert.Contains(race, p => p.PlayerId == playerId);
        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task Handle_AtRaceViolationThreshold_WithdrawsAndBroadcastsRaceWithdrawn()
    {
        var (handler, lobby, broadcaster, _, _, playerId, _) = await Build();

        for (var i = 0; i < RaceViolationThreshold; i++)
            await handler.Handle(Violation(lobby.LobbyId, playerId), CancellationToken.None);

        var race = await lobby.RaceAccess.GetSnapshotOrDefault(lobby.LobbyId);
        Assert.NotNull(race);
        Assert.DoesNotContain(race, p => p.PlayerId == playerId);
        Assert.Contains(broadcaster.Broadcasts, s => s.EventName == GameEvents.RaceWithdrawn);
    }

    [Fact]
    public async Task RaceAlreadyRemoved_DoesNotBroadcastRaceWithdrawn()
    {
        var (handler, lobby, broadcaster, _, _, playerId, _) = await Build();
        lobby.RaceAccess.Remove(lobby.LobbyId);

        for (var i = 0; i < RaceViolationThreshold; i++)
            await handler.Handle(Violation(lobby.LobbyId, playerId), CancellationToken.None);

        Assert.DoesNotContain(broadcaster.Broadcasts, s => s.EventName == GameEvents.RaceWithdrawn);
    }

    [Fact]
    public async Task Handle_AtStrikeThreshold_SuspendsRegisteredUserAndBroadcastsSuspended()
    {
        var (handler, lobby, broadcaster, bans, strikes, playerId, _) = await Build();

        for (var i = 0; i < SuspensionThreshold; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        Assert.Single(bans.Suspensions);
        Assert.Contains(broadcaster.Broadcasts, s => s.EventName == GameEvents.Suspended);
    }

    [Fact]
    public async Task Handle_GuestAtStrikeThreshold_NeverSuspends()
    {
        var (handler, lobby, broadcaster, bans, strikes, playerId, _) = await Build(registerPlayer: false);

        for (var i = 0; i < SuspensionThreshold; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        Assert.Empty(bans.Suspensions);
        Assert.DoesNotContain(broadcaster.Broadcasts, s => s.EventName == GameEvents.Suspended);
    }

    [Fact]
    public async Task Handle_SecondSuspension_IncrementsSuspensionCountAndLogsForHumanReview()
    {
        var (handler, lobby, _, bans, strikes, playerId, logger) = await Build();

        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < SuspensionThreshold; i++)
                await WithdrawFromRace(handler, lobby, strikes, playerId);

        Assert.Equal(2, bans.Suspensions.Count);
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    private static DomainEventNotification<RaceViolationEvent> Violation(Guid lobbyId, Guid playerId) =>
        new(new RaceViolationEvent(lobbyId, playerId, "alice", "test rule"));

    // Drives one full race-withdrawal incident (K rejected updates), then clears the
    // per-race count to simulate the player starting a fresh race in the same lobby.
    private static async Task WithdrawFromRace(RaceViolationHandler handler, LobbyTestContext lobby, StrikeRegistry strikes, Guid playerId)
    {
        for (var i = 0; i < RaceViolationThreshold; i++)
            await handler.Handle(Violation(lobby.LobbyId, playerId), CancellationToken.None);

        strikes.ClearRace(lobby.LobbyId);
    }

    private static async Task<(RaceViolationHandler Handler, LobbyTestContext Lobby, FakeBroadcaster Broadcaster, FakeBanRepository Bans, StrikeRegistry Strikes, Guid PlayerId, FakeLogger<RaceViolationHandler> Logger)> Build(bool registerPlayer = true)
    {
        User host = new User("host");
        var lobby = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(lobby, host.Id);

        // The host is registered and an actual race participant; a guest is neither
        // (an unregistered id with no race entry is enough to exercise the guest cap).
        var playerId = registerPlayer ? host.Id : Guid.NewGuid();

        var strikes = new StrikeRegistry();
        var broadcaster = new FakeBroadcaster();
        var bans = new FakeBanRepository();
        var logger = new FakeLogger<RaceViolationHandler>();
        var options = Options.Create(new AntiCheatSanctionOptions
        {
            RaceViolationThreshold = RaceViolationThreshold,
            SuspensionThreshold = SuspensionThreshold,
        });

        var handler = new RaceViolationHandler(strikes, lobby.RaceAccess, broadcaster, bans, lobby.UserRepo, new FakeSender(), options, logger);

        return (handler, lobby, broadcaster, bans, strikes, playerId, logger);
    }
}
