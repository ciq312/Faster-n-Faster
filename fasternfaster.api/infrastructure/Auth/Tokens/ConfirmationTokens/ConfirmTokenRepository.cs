using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Db.Tokens;

/// <summary>
/// repository responsible for verifaction tokens email and password reset
/// </summary>
/// <param name="dbContext"></param>
public class ConfirmTokenRepository(AppDbContext dbContext) : IConfirmTokenRepository
{
    private readonly AppDbContext dbContext = dbContext;
    public Task Add(Token token)
    {
        dbContext.Tokens.Add(token);
        return Task.CompletedTask;
    }

    public async Task<Token?> GetByValueAsync(string token)
    {
        return await dbContext.Tokens.FirstOrDefaultAsync(x => x.Value == token);
    }

    public async Task<Token?> GetLatestForUserAsync(Guid userId, TokenType type)
    {
        return await dbContext.Tokens
            .Where(x => x.UserId == userId && x.Type == type)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task Remove(Token token)
    {
        dbContext.Tokens.Remove(token);
        return Task.CompletedTask;
    }

    public Task RemoveAllForUser(Guid userId, TokenType type)
    {
        dbContext.Tokens.RemoveRange(dbContext.Tokens.Where(x => x.UserId == userId && x.Type == type));
        return Task.CompletedTask;
    }
    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}