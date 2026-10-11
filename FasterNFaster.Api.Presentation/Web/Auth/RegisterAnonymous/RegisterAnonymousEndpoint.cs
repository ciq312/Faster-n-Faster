using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Auth.RegisterAnonymous;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Auth.RegisterAnonymous;

public class RegisterAnonymousRequest
{
    public string Nick { get; set; } = null!;
}

public class RegisterAnonymousEndpoint(ISender sender, IAuthTokenWriter auth) : Endpoint<RegisterAnonymousRequest, RegisterAnonymousResponse>
{
    public override void Configure()
    {
        Post("/api/auth/guest");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthModerate));
    }

    public override async Task HandleAsync(RegisterAnonymousRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterAnonymousCommand(req.Nick), ct);

        auth.WriteGuestAuth(result.Tokens.AccessToken);

        await Send.CreatedAtAsync<RegisterAnonymousEndpoint>(
            routeValues: null,
            responseBody: new RegisterAnonymousResponse(result.UserId),
            cancellation: ct);
    }
}
