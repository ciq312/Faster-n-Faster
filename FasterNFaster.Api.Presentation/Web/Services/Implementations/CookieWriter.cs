using FasterNFaster.Api.UseCases.Auth;
using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class CookieWriter(IHttpContextAccessor httpContextAccessor, IOptions<AuthCookiesOptions> options) : IAuthTokenWriter
{
    private readonly AuthCookiesOptions options = options.Value;

    private HttpResponse Response =>
            httpContextAccessor.HttpContext?.Response
            ?? throw new InvalidOperationException("No HttpContext available");

    public void WriteAuth(TokenPair tokens)
    {
        WriteAccessTokenCookie(tokens.AccessToken);
        WriteRefreshTokenCookie(tokens.RefreshToken!);
    }

    public void WriteGuestAuth(IssuedToken accessToken)
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

    private void WriteRefreshTokenCookie(IssuedToken refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.CookieSameSite,
            Path = options.RefreshTokenPath,
            Expires = refreshToken.ExpiresAt
        };
        Response.Cookies.Append(options.RefreshTokenCookieName, refreshToken.Value, cookieOptions);
    }

    private void WriteAccessTokenCookie(IssuedToken accessToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.CookieSameSite,
            Expires = accessToken.ExpiresAt
        };
        Response.Cookies.Append(options.AccessTokenCookieName, accessToken.Value, cookieOptions);
    }

    private void WriteGuestAccessTokenCookie(IssuedToken guestAccessToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.CookieSameSite,
            Expires = guestAccessToken.ExpiresAt
        };
        Response.Cookies.Append(options.AccessTokenCookieName, guestAccessToken.Value, cookieOptions);
    }
}
