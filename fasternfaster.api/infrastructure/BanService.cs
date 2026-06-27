using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure;

public class BanService(AppDbContext db) : IBanService
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
