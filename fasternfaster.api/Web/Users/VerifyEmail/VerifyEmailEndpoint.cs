using FastEndpoints;
using FasterNFaster.Api.Extensions;
using FasterNFaster.Api.UseCases.Users.VerifyEmail;
using MediatR;

namespace FasterNFaster.Api.Web.Users.RegisterUser.EndPoints;

public class VerifyEmailEndpoint(ISender sender) : Endpoint<VerifyEmailRequest>
{
    public override void Configure()
    {
        Post("/api/auth/verify-email");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthModerate));
    }

    public override async Task HandleAsync(VerifyEmailRequest req, CancellationToken ct)
    {
        await sender.Send(new VerifyEmailCommand(req.Token), ct);
        await Send.OkAsync(cancellation: ct);
    }
}

public record VerifyEmailRequest(string Token);
