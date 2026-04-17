using FastEndpoints;
using FasterNFaster.Api.Web.Services;

namespace FasterNFaster.Api.Web.Users.RefreshToken;

public class RefreshTokenEndpoint(JwtTokenService tokenService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/auth/refresh");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = tokenService.TryRefresh(refreshToken);

        if (result is null)
        {
            tokenService.ClearTokenCookies(HttpContext.Response);
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var (accessToken, newRefreshToken) = result.Value;
        tokenService.SetAccessAndRefreshTokenCookies(HttpContext.Response, accessToken, newRefreshToken);

        await Send.OkAsync();
    }
}
