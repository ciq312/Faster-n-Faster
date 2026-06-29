using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Users.RefreshToken;

public class RefreshTokenEndpoint(ITokenService tokenService, ICookiesWriter cookies, IOptions<AuthCookiesOptions> options) : EndpointWithoutRequest
{
    private readonly ITokenService tokenService = tokenService;
    private readonly ICookiesWriter cookies = cookies;
    private readonly AuthCookiesOptions options = options.Value;
    public override void Configure()
    {
        Get("/api/auth/refresh");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies[options.RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var tokens = await tokenService.TryRefreshTokens(refreshToken);

        if (tokens is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        cookies.WriteAccessTokenCookie(tokens.AccessToken);
        cookies.WriteRefreshTokenCookie(tokens.RefreshToken!);
        await Send.OkAsync(cancellation: ct);
    }
}
