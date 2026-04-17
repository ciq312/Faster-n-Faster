using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities;
using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class InMemorySessionService() : ISessionService
{
    // later switch to redis
    private readonly ConcurrentDictionary<Guid, string> userSessions = new();
    public void ClearActiveSession(Guid userId)
    {
        userSessions.Remove(userId, out _);
    }

    public string? GetActiveSession(Guid userId) => userSessions.GetValueOrDefault(userId);

    public void SetUserSession(Guid userId, string sessionId)
    {
        userSessions[userId] = sessionId;
    }


}