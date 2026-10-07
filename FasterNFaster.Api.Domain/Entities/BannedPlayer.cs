using System.ComponentModel.DataAnnotations;

namespace FasterNFaster.Api.Core.Entities;

public class BannedPlayer : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public DateTime BannedAt { get; private set; }

    /// <summary>Null means a permanent ban.</summary>
    public DateTime? ExpiresAt { get; private set; }
    public int SuspensionCount { get; private set; }

    [StringLength(200)]
    public string? Reason { get; private set; }

    private BannedPlayer() { }

    public static BannedPlayer Create(Guid userId, string? reason)
    {
        return new BannedPlayer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BannedAt = DateTime.UtcNow,
            Reason = reason
        };
    }

    public static BannedPlayer CreateSuspension(Guid userId, string reason, DateTime expiresAt)
    {
        return new BannedPlayer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BannedAt = DateTime.UtcNow,
            Reason = reason,
            ExpiresAt = expiresAt,
            SuspensionCount = 1
        };
    }

    public void ExtendSuspension(string reason, DateTime expiresAt)
    {
        BannedAt = DateTime.UtcNow;
        Reason = reason;

        // A permanent ban must never be downgraded to a temporary suspension.
        if (ExpiresAt is null) return;

        ExpiresAt = expiresAt;
        SuspensionCount++;
    }

    public void MakePermanent(string? reason)
    {
        BannedAt = DateTime.UtcNow;
        Reason = reason;
        ExpiresAt = null;
    }
}
