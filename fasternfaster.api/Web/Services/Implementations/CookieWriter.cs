using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class CookieWriter(IHttpContextAccessor httpContextAccessor, IOptions<AuthCookiesOptions> options) : ICookiesWriter
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly AuthCookiesOptions options = options.Value;

    private HttpResponse Response =>
            httpContextAccessor.HttpContext?.Response
            ?? throw new InvalidOperationException("No HttpContext available");
    public void WriteRefreshTokenCookie(string token)
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

    public void WriteAccessTokenCookie(string token)
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

    public void WriteGuestAccessTokenCookie(string token)
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

    public void DeleteTokensCookies()
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
}