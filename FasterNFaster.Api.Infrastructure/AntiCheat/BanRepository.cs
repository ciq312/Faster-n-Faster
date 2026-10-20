using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.UseCases.AntiCheat.Ports;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.AntiCheat;

public class BanRepository(AppDbContext db, TimeProvider timeProvider) : IBanRepository
{
    public async Task<bool> IsBannedAsync(Guid userId)
    {
        var now = timeProvider.GetUtcNow();
        return await db.BannedPlayers.AsNoTracking().AnyAsync(b => b.UserId == userId && (b.ExpiresAt == null || b.ExpiresAt > now));
    }

    public async Task BanAsync(Guid userId, string? reason)
    {
        var now = timeProvider.GetUtcNow();
        var existing = await db.BannedPlayers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (existing is null)
            db.BannedPlayers.Add(new BannedPlayer(userId, reason, now));
        else
            existing.MakePermanent(reason, now);

        await db.SaveChangesAsync();
    }

    public async Task<int> SuspendAsync(Guid userId, string reason, DateTimeOffset expiresAt)
    {
        var now = timeProvider.GetUtcNow();
        var existing = await db.BannedPlayers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (existing is null)
        {
            existing = new BannedPlayer(userId, reason, expiresAt, now);
            db.BannedPlayers.Add(existing);
        }
        else
        {
            existing.ExtendSuspension(reason, expiresAt, now);
        }

        await db.SaveChangesAsync();
        return existing.SuspensionCount;
    }
}
