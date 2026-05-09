using System.ComponentModel.DataAnnotations;

namespace FasterNFaster.Api.Core.Entities;

public class BannedPlayer
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime BannedAt { get; private set; }

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
}
