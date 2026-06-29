using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Commands;
using FasterNFaster.Api.Web.Services.Interfaces;
using MediatR;

namespace FasterNFaster.Api.Web.Users.RegisterAnonymous.Endpoints;

public class RegisterAnonymousRequest
{
    public string Nick { get; set; } = null!;
}

public class RegisterAnonymousEndpoint(ISender sender, ICookiesWriter cookies) : Endpoint<RegisterAnonymousRequest, RegisterAnonymousResponse>
{
    private readonly ISender sender = sender;
    private readonly ICookiesWriter cookies = cookies;

    public override void Configure()
    {
        Post("/api/auth/guest");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterAnonymousRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterAnonymousCommand(req.Nick), ct);

        cookies.DeleteTokensCookies();
        cookies.WriteGuestAccessTokenCookie(result.Tokens.AccessToken);

        await Send.CreatedAtAsync<RegisterAnonymousEndpoint>(
            routeValues: null,
            responseBody: new RegisterAnonymousResponse(result.UserId),
            cancellation: ct);
    }
}
