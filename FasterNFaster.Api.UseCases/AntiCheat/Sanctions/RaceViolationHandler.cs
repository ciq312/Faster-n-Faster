using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.UseCases.AntiCheat.Sanctions;

public class RaceViolationHandler(
    IStrikeRegistry strikes,
    IRaceStore races,
    IBroadcaster broadcaster,
    IBanRepository bans,
    IUserRepository users,
    ISender sender,
    IPublisher publisher,
    IOptions<AntiCheatSanctionOptions> options,
    ILogger<RaceViolationHandler> logger,
    TimeProvider timeProvider) : INotificationHandler<DomainEventNotification<RaceViolationEvent>>
{
    private readonly AntiCheatSanctionOptions opts = options.Value;

    public async Task Handle(DomainEventNotification<RaceViolationEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        logger.LogWarning("Anti-cheat violation for player {PlayerId} ({Nick}) in lobby {LobbyId}: {Rule} {TelemetryOperation}",
            e.PlayerId, e.Nick, e.LobbyId, e.Rule, TelemetryOperations.RaceViolation);

        var raceViolationCount = strikes.RecordRaceViolation(e.LobbyId, e.PlayerId);
        if (raceViolationCount < opts.RaceViolationThreshold) return;

        if (await races.TryMutate(e.LobbyId, r => r.WithdrawParticipant(e.PlayerId)))
        {
            await broadcaster.Broadcast(Audience.Player(e.PlayerId), c => c.RaceWithdrawn());
            logger.LogWarning("Player {PlayerId} was withdrawn from race in lobby {LobbyId} after repeated violations {TelemetryOperation}",
                e.PlayerId,
                e.LobbyId,
                TelemetryOperations.PlayerWithdrawnForViolation);
        }

        // A strike is one incident that reached the withdrawal threshold, not one rejected update,
        // and it counts even if the race is already gone, so leaving can't dodge it.
        var strikeCount = strikes.RecordStrike(e.PlayerId, timeProvider.GetUtcNow(), opts.StrikeWindow);
        if (strikeCount < opts.SuspensionThreshold) return;

        // Guests cap at race withdrawal: no durable identity to suspend against.
        if (!await users.IsUserRegistered(e.PlayerId)) return;

        var expiresAt = timeProvider.GetUtcNow().Add(opts.SuspensionDuration);
        var suspensionCount = await bans.SuspendAsync(e.PlayerId, $"Repeated anti-cheat violations ({e.Rule})", expiresAt);
        strikes.ClearStrikes(e.PlayerId);
        logger.LogWarning("Player {PlayerId} was suspended in lobby {LobbyId} after repeated violations (suspension #{SuspensionCount}) {TelemetryOperation}",
            e.PlayerId,
            e.LobbyId,
            suspensionCount,
            TelemetryOperations.PlayerSuspended);

        // Termination is the only thing that closes a suspended player's live connection,
        // so a failed broadcast or disconnect must not skip it.
        try
        {
            await broadcaster.Broadcast(Audience.Player(e.PlayerId), c => c.Suspended(new SuspendedDTO(expiresAt)));
            await sender.Send(new DisconnectCommand(e.PlayerId), cancellationToken);
        }
        finally
        {
            await publisher.Publish(new PlayerSanctionedEvent(e.PlayerId), cancellationToken);
        }

        await bans.BanAsync(e.PlayerId, "Several strikes recorded");
        logger.LogWarning("Player {PlayerId} was banned while playing in lobby {LobbyId} for cheating {TelemetryOperation}",
            e.PlayerId,
            e.LobbyId,
            TelemetryOperations.PlayerBanned);

    }
}
