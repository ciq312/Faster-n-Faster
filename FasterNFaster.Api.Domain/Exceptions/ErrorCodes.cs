namespace FasterNFaster.Api.Domain.Exceptions;

public static class ErrorCodes
{
    public const string EmailNotVerified = "email_not_verified";
    public const string NotAuthenticated = "not_authenticated";
    public const string InvalidCredentials = "invalid_credentials";
    public const string Forbidden = "forbidden";
    public const string RateLimited = "rate_limited";
    public const string NotOnLeaderboard = "not_on_leaderboard";
    public const string InvalidInviteCode = "invalid_invite_code";
    public const string InvalidCursor = "invalid_cursor";
}
