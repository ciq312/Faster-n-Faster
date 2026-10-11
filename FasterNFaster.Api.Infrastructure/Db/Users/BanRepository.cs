using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Db.Users;

public class BanRepository(AppDbContext db) : IBanRepository
{
    public Task<bool> IsBannedAsync(Guid userId) =>
        db.BannedPlayers.AsNoTracking().AnyAsync(b => b.UserId == userId && (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow));

    public async Task BanAsync(Guid userId, string? reason)
    {
        var existing = await db.BannedPlayers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (existing is null)
            db.BannedPlayers.Add(BannedPlayer.Create(userId, reason));
        else
            existing.MakePermanent(reason);

        await db.SaveChangesAsync();
    }

    public async Task<int> SuspendAsync(Guid userId, string reason, DateTime expiresAt)
    {
        var existing = await db.BannedPlayers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (existing is null)
        {
            existing = BannedPlayer.CreateSuspension(userId, reason, expiresAt);
            db.BannedPlayers.Add(existing);
        }
        else
        {
            existing.ExtendSuspension(reason, expiresAt);
        }

        await db.SaveChangesAsync();
        return existing.SuspensionCount;
    }
}
