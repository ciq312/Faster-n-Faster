using FasterNFaster.Api.UseCases.Auth.Tokens;
using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class CookieWriter(IHttpContextAccessor httpContextAccessor, IOptions<AuthCookiesOptions> options) : IAuthTokenWriter
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly AuthCookiesOptions options = options.Value;

    private HttpResponse Response =>
            httpContextAccessor.HttpContext?.Response
            ?? throw new InvalidOperationException("No HttpContext available");

    public void WriteAuth(TokenPair tokens)
    {
        WriteAccessTokenCookie(tokens.AccessToken);
        WriteRefreshTokenCookie(tokens.RefreshToken!);
    }

    public void WriteGuestAuth(string accessToken)
    {
        ClearAuth();
        WriteGuestAccessTokenCookie(accessToken);
    }

    public void ClearAuth()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.CookieSameSite,
            Path = "/"
        };
        Response.Cookies.Delete(options.AccessTokenCookieName, cookieOptions);
        Response.Cookies.Delete(options.RefreshTokenCookieName, new CookieOptions { Path = options.RefreshTokenPath });
    }

    private void WriteRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.CookieSameSite,
            Path = options.RefreshTokenPath,
            Expires = DateTime.UtcNow.Add(options.RefreshTokenExpiry)
        };
        Response.Cookies.Append(options.RefreshTokenCookieName, token, cookieOptions);
    }

    private void WriteAccessTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.CookieSameSite,
            Expires = DateTime.UtcNow.Add(options.AccessTokenExpiry)
        };
        Response.Cookies.Append(options.AccessTokenCookieName, token, cookieOptions);
    }

    private void WriteGuestAccessTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.CookieSameSite,
            Expires = DateTime.UtcNow.Add(options.GuestAccessTokenExpiry)
        };
        Response.Cookies.Append(options.AccessTokenCookieName, token, cookieOptions);
    }
}
