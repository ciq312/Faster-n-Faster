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
        WriteAccessTokenCookie(accessToken);
    }

    public void ClearAuth()
    {
        Response.Cookies.Delete(options.AccessTokenCookieName, BaseCookieOptions());

        var refreshCookieOptions = BaseCookieOptions();
        refreshCookieOptions.Path = options.RefreshTokenPath;
        Response.Cookies.Delete(options.RefreshTokenCookieName, refreshCookieOptions);
    }

    private void WriteRefreshTokenCookie(IssuedToken refreshToken)
    {
        var cookieOptions = BaseCookieOptions();
        cookieOptions.Path = options.RefreshTokenPath;
        cookieOptions.Expires = refreshToken.ExpiresAt;
        Response.Cookies.Append(options.RefreshTokenCookieName, refreshToken.Value, cookieOptions);
    }

    private void WriteAccessTokenCookie(IssuedToken accessToken)
    {
        var cookieOptions = BaseCookieOptions();
        cookieOptions.Expires = accessToken.ExpiresAt;
        Response.Cookies.Append(options.AccessTokenCookieName, accessToken.Value, cookieOptions);
    }

    private CookieOptions BaseCookieOptions() => new()
    {
        HttpOnly = options.HttpOnly,
        Secure = options.Secure,
        SameSite = options.CookieSameSite,
        Path = "/"
    };
}
