using FastEndpoints;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.Web.Users.RefreshToken;

public class RefreshTokenEndpoint(ITokenService tokenService) : EndpointWithoutRequest
{
    private readonly ITokenService tokenService = tokenService;
    public override void Configure()
    {
        Get("/api/auth/refresh");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies[Config["AuthCookies:RefreshTokenCookieName"]!];
#if DEBUG
        Log.Information("Received refresh token request with token: {RefreshToken}", refreshToken);
#endif
        if (string.IsNullOrEmpty(refreshToken))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (await tokenService.TryRefreshToken(refreshToken))
        {
            await Send.OkAsync(cancellation: ct);
        }

        else
            await Send.UnauthorizedAsync(ct);

    }
}
