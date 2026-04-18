namespace FasterNFaster.Api.Web.Options.AuthCookiesOptions;


public class AuthCookiesOptions
{
    public string AccessTokenCookieName { get; set; } = "access_token";
    public string RefreshTokenCookieName { get; set; } = "refresh_token";
    public bool Secure { get; set; } = false;
    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Strict;
    public string RefreshTokenPath { get; set; } = "/api/auth/refresh";
    public bool HttpOnly { get; set; } = true;
    public TimeSpan GuestAccessTokenExpiry { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan AccessTokenExpiry { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RefreshTokenExpiry { get; set; } = TimeSpan.FromHours(4);
}