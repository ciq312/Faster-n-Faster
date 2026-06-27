using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class InMemorySessionService(ITokenStore tokenStore) : ISessionService
{
    private readonly ConcurrentDictionary<Guid, string> userSessions = new();

    public void ClearActiveSession(Guid userId) => userSessions.Remove(userId, out _);

    public string? GetActiveSession(Guid userId) => userSessions.GetValueOrDefault(userId);

    public void SetUserSession(Guid userId, string sessionId) => userSessions[userId] = sessionId;

    public async Task InvalidateAll(Guid userId)
    {
        userSessions.Remove(userId, out _);
        await tokenStore.DeleteAllTokensForUserAsync(userId);
    }
}
