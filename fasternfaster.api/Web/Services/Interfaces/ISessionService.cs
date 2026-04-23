public interface ISessionService
{
    public void SetUserSession(Guid userId, string sessionId);
    public string? GetActiveSession(Guid userId);

    public void ClearActiveSession(Guid userId);

    // Removes the in-memory session entry AND revokes all stored refresh tokens for the user.
    // Endpoints call this on account switching so a single hop invalidates everything at once.
    public Task InvalidateAll(Guid userId);
}