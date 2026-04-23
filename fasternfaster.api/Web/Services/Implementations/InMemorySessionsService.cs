using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class InMemorySessionService(ITokenStore tokenStore) : ISessionService
{
    // later switch to redis
    private readonly ConcurrentDictionary<Guid, string> userSessions = new();
    private readonly ITokenStore tokenStore = tokenStore;

    public void ClearActiveSession(Guid userId)
    {
        userSessions.Remove(userId, out _);
    }

    public string? GetActiveSession(Guid userId) => userSessions.GetValueOrDefault(userId);

    public void SetUserSession(Guid userId, string sessionId)
    {
        userSessions[userId] = sessionId;
    }

    public async Task InvalidateAll(Guid userId)
    {
        userSessions.Remove(userId, out _);
        await tokenStore.DeleteAllForUserAsync(userId);
    }
}
