using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.Logout;
using FasterNFaster.Api.Web.Options.AuthCookiesOptions;
using FasterNFaster.Api.Web.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Users.Logout;

public class LogoutEndpoint(
    ISender sender,
    IAuthTokenWriter auth,
    IOptions<AuthCookiesOptions> options
    ) : EndpointWithoutRequest
{
    private readonly AuthCookiesOptions options = options.Value;

    public override void Configure()
    {
        Post("/api/auth/logout");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies[options.RefreshTokenCookieName];
        Guid? userId = Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

        await sender.Send(new LogoutCommand(refreshToken, userId), ct);

        auth.ClearAuth();
        await Send.OkAsync(cancellation: ct);
    }
}
