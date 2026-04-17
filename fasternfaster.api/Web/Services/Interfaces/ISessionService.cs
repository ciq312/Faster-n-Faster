public interface ISessionService
{
    public void SetUserSession(Guid userId, string sessionId);
    public string? GetActiveSession(Guid userId);

    public void ClearActiveSession(Guid userId);
}