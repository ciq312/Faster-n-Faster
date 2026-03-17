using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Data;

public class LobbyRepository : ILobbyRepository
{
    private readonly AppDbContext _db;

    public LobbyRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> NameExistsAsync(string name) =>
        _db.Lobbies.AnyAsync(l => l.Name == name && l.Status != "finished");

    public Task<bool> InviteCodeExistsAsync(string code) =>
        _db.Lobbies.AnyAsync(l => l.InviteCode == code);

    public async Task AddAsync(Lobby lobby)
    {
        _db.Lobbies.Add(lobby);
        await _db.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
