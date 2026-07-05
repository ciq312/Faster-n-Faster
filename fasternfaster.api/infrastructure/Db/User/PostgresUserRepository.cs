using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.EntityFrameworkCore;
using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Infrastructure.Db.Users;

public class PostgresUserRepository(AppDbContext context) : IUserRepository
{
    private readonly AppDbContext appDbContext = context;

    public async Task AddAsync(User user)
    {
        appDbContext.Users.Add(user);
        await appDbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        appDbContext.Users.Update(user);
        await appDbContext.SaveChangesAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await appDbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await appDbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<bool> DoUserExistByNickAsync(string nick)
    {
        return await appDbContext.Users.AnyAsync(x => x.Nick == nick);
    }


    public async Task<User?> GetUserByLoginAsync(string login)
    {
        return await appDbContext.Users.FirstOrDefaultAsync(x => x.Login == login);
    }

    public async Task<bool> IsUserRegistred(Guid userId)
    {
        if (await GetByIdAsync(userId) == null) return false;
        return true;
    }

}