using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Db.Users;

public class BanRepository(AppDbContext db) : IBanRepository
{
    public Task<bool> IsBannedAsync(Guid userId) =>
        db.BannedPlayers.AsNoTracking().AnyAsync(b => b.UserId == userId);

    public async Task BanAsync(Guid userId, string? reason)
    {
        if (await IsBannedAsync(userId)) return;

        db.BannedPlayers.Add(BannedPlayer.Create(userId, reason));
        await db.SaveChangesAsync();
    }
}
