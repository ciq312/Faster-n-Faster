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

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await appDbContext.Users.FindAsync(id);

    }
    public async Task<User?> GetByTokenAsync(string token)
    {
        return await appDbContext.Users.FirstOrDefaultAsync(u => u.Token == token);
    }
    public async Task<bool> DoUserExistByLoginAsync(string login)
    {
        return await appDbContext.Users.AnyAsync(x => x.Login == login);
    }

    public async Task<bool> DoUserExistByNickAsync(string nick)
    {
        return await appDbContext.Users.AnyAsync(x => x.Nick == nick);
    }


    public async Task<User?> GetUserByLoginAsync(string login)
    {
        return await appDbContext.Users.FirstOrDefaultAsync(x => x.Login == login);
    }

}