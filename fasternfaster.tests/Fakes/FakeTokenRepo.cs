using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;

namespace FasterNFaster.Tests.Fakes;

public class FakeTokenRepo : IConfirmTokenRepository
{
    public List<Token> tokens = new();

    public Task Add(Token token)
    {
        tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<Token?> GetByValueAsync(string token)
    {
        return Task.FromResult(tokens.FirstOrDefault(x => x.Value == token));
    }

    public Task<Token?> GetLatestForUserAsync(Guid userId, TokenType type)
    {
        var latest = tokens
            .Where(x => x.UserId == userId && x.Type == type)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult<Token?>(latest);
    }

    public Task Remove(Token token)
    {
        tokens.Remove(token);
        return Task.CompletedTask;
    }

    public Task RemoveAllForUser(Guid userId, TokenType type)
    {
        tokens.RemoveAll(x => x.UserId == userId && x.Type == type);
        return Task.CompletedTask;
    }
}
