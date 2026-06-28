using System.Data;
using FastEndpoints;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Interfaces;

namespace FasterNFaster.Api.Web.Users.RegisterAnonymous.Endpoints;

public class RegisterAnonymousRequest
{
    public string Nick { get; set; } = null!;
}

public class RegisterAnonymousEndpoint(ITokenService tokenService, ICookiesWriter cookies) : Endpoint<RegisterAnonymousRequest, RegisterAnonymousResult>
{
    private readonly ITokenService tokenService = tokenService;
    private readonly ICookiesWriter cookies = cookies;

    public override void Configure()
    {
        Post("/api/auth/guest");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterAnonymousRequest req, CancellationToken ct)
    {
        var GuestId = Guid.NewGuid();

        var tokens = tokenService.IssueGuestTokens(GuestId, req.Nick);

        cookies.DeleteTokensCookies();
        cookies.WriteGuestAccessTokenCookie(tokens.AccessToken);

        await Send.CreatedAtAsync<RegisterAnonymousEndpoint>(new { GuestId = GuestId }, cancellation: ct);
    }
}
