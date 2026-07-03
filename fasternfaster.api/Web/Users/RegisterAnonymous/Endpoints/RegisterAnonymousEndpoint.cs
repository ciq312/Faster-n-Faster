using FastEndpoints;
using FasterNFaster.Api.Extensions;
using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Commands;
using FasterNFaster.Api.Web.Services.Interfaces;
using MediatR;

namespace FasterNFaster.Api.Web.Users.RegisterAnonymous.Endpoints;

public class RegisterAnonymousRequest
{
    public string Nick { get; set; } = null!;
}

public class RegisterAnonymousEndpoint(ISender sender, IAuthTokenWriter auth) : Endpoint<RegisterAnonymousRequest, RegisterAnonymousResponse>
{
    private readonly ISender sender = sender;
    private readonly IAuthTokenWriter auth = auth;

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
