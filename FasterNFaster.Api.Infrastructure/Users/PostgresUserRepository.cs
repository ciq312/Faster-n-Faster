using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.UseCases.Users.Ports;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Users;

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

    public Task<bool> IsUserRegistered(Guid userId) => appDbContext.Users.AnyAsync(u => u.Id == userId);
}
