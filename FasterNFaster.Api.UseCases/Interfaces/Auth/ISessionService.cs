namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface ISessionService
{
    void SetUserSession(Guid userId, string sessionId);
    string? GetActiveSession(Guid userId);

    // Removes the entry only if it still points at this exact sessionId, so a stale
    // disconnect can never wipe a newer session the user already reconnected with.
    void ClearSessionIfActive(Guid userId, string sessionId);

    // Removes the in-memory session entry AND revokes all stored refresh tokens for the user.
    // Endpoints call this on account switching so a single hop invalidates everything at once.
    Task RevokeAllSessions(Guid userId);
}
