namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface ISessionService
{
    void SetUserSession(Guid userId, string sessionId);
    string? GetActiveSession(Guid userId);

    void ClearActiveSession(Guid userId);

    // Removes the in-memory session entry AND revokes all stored refresh tokens for the user.
    // Endpoints call this on account switching so a single hop invalidates everything at once.
    Task RevokeAllSessions(Guid userId);
}
