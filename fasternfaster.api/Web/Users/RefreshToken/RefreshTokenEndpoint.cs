using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.RefreshToken.Commands;
using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Users.RefreshToken;

public class RefreshTokenEndpoint(ISender sender, IAuthTokenWriter auth, IOptions<AuthCookiesOptions> options) : EndpointWithoutRequest
{
    private readonly ISender sender = sender;
    private readonly IAuthTokenWriter auth = auth;
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

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), ct);

        auth.WriteAuth(result.Tokens);
        await Send.OkAsync(cancellation: ct);
    }
}
