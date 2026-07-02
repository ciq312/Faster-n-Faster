namespace FasterNFaster.Api.Infrastructure.Auth;

public class JwtOptions
{
    public string JWT_PRIVATE_TOKEN { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan GuestAccessTokenLifetime { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromHours(4);
    public bool SlidingRefreshExpiration { get; set; } = true;
    public string GuestRole { get; set; } = "Guest";
    public string PlayerRole { get; set; } = "Player";
}
