using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Auth.Logout;
using MediatR;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Presentation.Web.Auth.Logout;

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
