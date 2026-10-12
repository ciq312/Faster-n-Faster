using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.Infrastructure.AntiCheat;
using FasterNFaster.Api.UseCases.AntiCheat.Sanctions;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FasterNFaster.UnitTests.AntiCheat;

public class RaceViolationHandlerTests
{
    private const int RaceViolationThreshold = 3;
    private const int SuspensionThreshold = 3;

    [Fact]
    public async Task Handle_BelowRaceViolationThreshold_DoesNotWithdrawOrBroadcast()
    {
        var (handler, lobby, broadcaster, _, _, playerId, _, _) = await Build();

        await handler.Handle(Violation(lobby.LobbyId, playerId), CancellationToken.None);

        var race = await lobby.ReadRace(r => r.GetSnapshot());
        Assert.NotNull(race);
        Assert.Contains(race, p => p.PlayerId == playerId);
        Assert.Empty(broadcaster.Broadcasts);
    }

    [Fact]
    public async Task Handle_AtRaceViolationThreshold_WithdrawsAndBroadcastsRaceWithdrawn()
    {
        var (handler, lobby, broadcaster, _, _, playerId, _, _) = await Build();

        for (var i = 0; i < RaceViolationThreshold; i++)
            await handler.Handle(Violation(lobby.LobbyId, playerId), CancellationToken.None);

        var race = await lobby.ReadRace(r => r.GetSnapshot());
        Assert.NotNull(race);
        Assert.DoesNotContain(race, p => p.PlayerId == playerId);
        Assert.Contains(broadcaster.Broadcasts, s => s.EventName == GameEvents.RaceWithdrawn);
    }

    [Fact]
    public async Task RaceAlreadyRemoved_DoesNotBroadcastRaceWithdrawn()
    {
        var (handler, lobby, broadcaster, _, _, playerId, _, _) = await Build();
        lobby.Races.Remove(lobby.LobbyId);

        for (var i = 0; i < RaceViolationThreshold; i++)
            await handler.Handle(Violation(lobby.LobbyId, playerId), CancellationToken.None);

        Assert.DoesNotContain(broadcaster.Broadcasts, s => s.EventName == GameEvents.RaceWithdrawn);
    }

    [Fact]
    public async Task Handle_AtStrikeThreshold_SuspendsRegisteredUserAndBroadcastsSuspended()
    {
        var (handler, lobby, broadcaster, bans, strikes, playerId, _, _) = await Build();

        for (var i = 0; i < SuspensionThreshold; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        Assert.Single(bans.Suspensions);
        Assert.Contains(broadcaster.Broadcasts, s => s.EventName == GameEvents.Suspended);
    }

    [Fact]
    public async Task ThresholdReached_SuspensionExpiresDurationAfterNow()
    {
        var (handler, lobby, _, bans, strikes, playerId, _, _) = await Build();
        lobby.Time.Advance(TimeSpan.FromMinutes(7));

        for (var i = 0; i < SuspensionThreshold; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        var suspension = Assert.Single(bans.Suspensions);
        Assert.Equal(lobby.Time.GetUtcNow() + new AntiCheatSanctionOptions().SuspensionDuration, suspension.ExpiresAt);
    }

    [Fact]
    public async Task Handle_GuestAtStrikeThreshold_NeverSuspends()
    {
        var (handler, lobby, broadcaster, bans, strikes, playerId, _, _) = await Build(registerPlayer: false);

        for (var i = 0; i < SuspensionThreshold; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        Assert.Empty(bans.Suspensions);
        Assert.DoesNotContain(broadcaster.Broadcasts, s => s.EventName == GameEvents.Suspended);
    }

    [Fact]
    public async Task AtStrikeThreshold_PublishesPlayerSanctioned()
    {
        var (handler, lobby, _, _, strikes, playerId, _, publisher) = await Build();

        for (var i = 0; i < SuspensionThreshold; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        var sanctioned = Assert.IsType<PlayerSanctionedEvent>(Assert.Single(publisher.Published));
        Assert.Equal(playerId, sanctioned.PlayerId);
    }

    [Fact]
    public async Task FailingDisconnect_StillPublishesPlayerSanctioned()
    {
        var sender = new FakeSender { OnSend = _ => throw new InvalidOperationException() };
        var (handler, lobby, _, _, strikes, playerId, _, publisher) = await Build(sender: sender);

        for (var i = 0; i < SuspensionThreshold - 1; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => WithdrawFromRace(handler, lobby, strikes, playerId));

        Assert.IsType<PlayerSanctionedEvent>(Assert.Single(publisher.Published));
    }

    [Fact]
    public async Task BelowStrikeThreshold_DoesNotPublishPlayerSanctioned()
    {
        var (handler, lobby, _, _, strikes, playerId, _, publisher) = await Build();

        for (var i = 0; i < SuspensionThreshold - 1; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task GuestAtStrikeThreshold_DoesNotPublishPlayerSanctioned()
    {
        var (handler, lobby, _, _, strikes, playerId, _, publisher) = await Build(registerPlayer: false);

        for (var i = 0; i < SuspensionThreshold; i++)
            await WithdrawFromRace(handler, lobby, strikes, playerId);

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task Handle_SecondSuspension_IncrementsSuspensionCountAndLogsForHumanReview()
    {
        var (handler, lobby, _, bans, strikes, playerId, logger, _) = await Build();

        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = 0; i < SuspensionThreshold; i++)
                await WithdrawFromRace(handler, lobby, strikes, playerId);
        }

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

    private static async Task<(RaceViolationHandler Handler, LobbyTestContext Lobby, FakeBroadcaster Broadcaster, FakeBanRepository Bans, StrikeRegistry Strikes, Guid PlayerId, FakeLogger<RaceViolationHandler> Logger, FakePublisher Publisher)> Build(bool registerPlayer = true, FakeSender? sender = null)
    {
        var host = new User("host");
        var lobby = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(lobby, host.Id);

        // The host is registered and an actual race participant; a guest is neither
        // (an unregistered id with no race entry is enough to exercise the guest cap).
        var playerId = registerPlayer ? host.Id : Guid.NewGuid();

        var strikes = new StrikeRegistry();
        var broadcaster = new FakeBroadcaster();
        var bans = new FakeBanRepository();
        var logger = new FakeLogger<RaceViolationHandler>();
        var publisher = new FakePublisher();
        var options = Options.Create(new AntiCheatSanctionOptions
        {
            RaceViolationThreshold = RaceViolationThreshold,
            SuspensionThreshold = SuspensionThreshold,
        });

        var handler = new RaceViolationHandler(strikes, lobby.Races, broadcaster, bans, lobby.UserRepo, sender ?? new FakeSender(), publisher, options, logger, lobby.Time);

        return (handler, lobby, broadcaster, bans, strikes, playerId, logger, publisher);
    }
}
