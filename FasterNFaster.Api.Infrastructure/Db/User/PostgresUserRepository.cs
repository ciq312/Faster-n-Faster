using FasterNFaster.Api.UseCases.Interfaces.Users;
using Microsoft.EntityFrameworkCore;
using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Infrastructure.Db.Users;

public class PostgresUserRepository(AppDbContext appDbContext) : IUserRepository
{
    public void Add(User user)
    {
        appDbContext.Users.Add(user);
    }

    public void Update(User user)
    {
        appDbContext.Users.Update(user);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await appDbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        return await appDbContext.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
    }

    public async Task<User?> GetUserByLoginAsync(string login)
    {
        var normalizedLogin = User.NormalizeLogin(login);
        return await appDbContext.Users.FirstOrDefaultAsync(x => x.Login == normalizedLogin);
    }

    public async Task<bool> IsUserRegistred(Guid userId)
    {
        if (await GetByIdAsync(userId) == null) return false;
        return true;
    }

}