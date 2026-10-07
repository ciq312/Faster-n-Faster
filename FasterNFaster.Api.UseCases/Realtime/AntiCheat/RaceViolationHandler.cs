using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Lobbies.Disconnect;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.UseCases.Realtime.AntiCheat;

public class RaceViolationHandler(
    IStrikeRegistry strikes,
    IRaceAccess races,
    IBroadcaster broadcaster,
    IBanRepository bans,
    IUserRepository users,
    ISender sender,
    IOptions<AntiCheatSanctionOptions> options,
    ILogger<RaceViolationHandler> logger) : INotificationHandler<DomainEventNotification<RaceViolationEvent>>
{
    private readonly AntiCheatSanctionOptions opts = options.Value;

    public async Task Handle(DomainEventNotification<RaceViolationEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;

        logger.LogWarning("Anti-cheat violation for player {PlayerId} ({Nick}) in lobby {LobbyId}: {Rule}",
            e.PlayerId, e.Nick, e.LobbyId, e.Rule);

        var raceViolationCount = strikes.RecordRaceViolation(e.LobbyId, e.PlayerId);
        if (raceViolationCount < opts.RaceViolationThreshold) return;

        await races.Mutate(e.LobbyId, r => r.WithdrawParticipant(e.PlayerId));
        await broadcaster.Broadcast(Audience.Player(e.PlayerId), GameEvents.RaceWithdrawn);

        // A strike is one race withdrawal, not one rejected update, so this only fires here.
        var strikeCount = strikes.RecordStrike(e.PlayerId, DateTime.UtcNow, opts.StrikeWindow);
        if (strikeCount < opts.SuspensionThreshold) return;

        // Guests cap at race withdrawal: no durable identity to suspend against.
        if (!await users.IsUserRegistred(e.PlayerId)) return;

        var expiresAt = DateTime.UtcNow.Add(opts.SuspensionDuration);
        var suspensionCount = await bans.SuspendAsync(e.PlayerId, $"Repeated anti-cheat violations ({e.Rule})", expiresAt);
        strikes.ClearStrikes(e.PlayerId);

        await broadcaster.Broadcast(Audience.Player(e.PlayerId), GameEvents.Suspended, new SuspendedDTO(expiresAt));
        await sender.Send(new DisconnectCommand(e.PlayerId), cancellationToken);

        if (suspensionCount > 1)
            logger.LogError("Player {PlayerId} suspended again (suspension #{SuspensionCount}) - flagged for human review", e.PlayerId, suspensionCount);
    }
}
