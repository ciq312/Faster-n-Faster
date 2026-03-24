using FasterNFaster.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure;

public class PostgresUserRepository : IUserRepository
{
    private readonly AppDbContext appDbContext;

    public PostgresUserRepository(AppDbContext context)
    {
        appDbContext = context;
    }

    public async Task AddAsync(User user)
    {
        appDbContext.Users.Add(user);
        await appDbContext.SaveChangesAsync();
    }

    public async Task<User?> GetAsync(Guid id)
    {
        return await appDbContext.Users.FindAsync(id);

    }

    public async Task<User?> GetByTokenAsync(string token)
    {
        return await appDbContext.Users.FirstOrDefaultAsync(u => u.Token == token);
    }
}