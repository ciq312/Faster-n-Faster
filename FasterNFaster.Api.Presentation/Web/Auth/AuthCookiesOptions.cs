namespace FasterNFaster.Api.Presentation.Web.Auth;

public class AuthCookiesOptions
{
    public string AccessTokenCookieName { get; set; } = "access_token";
    public string RefreshTokenCookieName { get; set; } = "refresh_token";
    public bool Secure { get; set; } = false;
    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Strict;
    public string RefreshTokenPath { get; set; } = "/api/auth";
    public bool HttpOnly { get; set; } = true;
}
