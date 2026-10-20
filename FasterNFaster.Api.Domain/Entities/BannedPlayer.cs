namespace FasterNFaster.Api.Domain.Entities;

public class BannedPlayer : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public DateTimeOffset BannedAt { get; private set; }

    /// <summary>Null means a permanent ban.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }
    public int SuspensionCount { get; private set; }

    public string? Reason { get; private set; }

    public BannedPlayer(Guid userId, string? reason, DateTimeOffset bannedAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        BannedAt = bannedAt;
        Reason = reason;
    }

    public BannedPlayer(Guid userId, string reason, DateTimeOffset expiresAt, DateTimeOffset bannedAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        BannedAt = bannedAt;
        Reason = reason;
        ExpiresAt = expiresAt;
        SuspensionCount = 1;
    }

    public void ExtendSuspension(string reason, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        BannedAt = now;
        Reason = reason;

        // A permanent ban must never be downgraded to a temporary suspension.
        if (ExpiresAt is null) return;

        ExpiresAt = expiresAt;
        SuspensionCount++;
    }

    public void MakePermanent(string? reason, DateTimeOffset now)
    {
        BannedAt = now;
        Reason = reason;
        ExpiresAt = null;
    }
}
