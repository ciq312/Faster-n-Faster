using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Auth.ResendVerification;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Auth.ResendVerification;

public class ResendVerificationEndpoint(ISender sender) : Endpoint<ResendVerificationRequest>
{
    public override void Configure()
    {
        Post("/api/auth/resend-verification");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthStrict));
    }

    public override async Task HandleAsync(ResendVerificationRequest req, CancellationToken ct)
    {
        await sender.Send(new ResendVerificationCommand(req.Email), ct);
        await Send.OkAsync(cancellation: ct);
    }
}

public record ResendVerificationRequest(string Email);
