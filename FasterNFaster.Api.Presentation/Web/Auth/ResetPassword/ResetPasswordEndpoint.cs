using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Auth.ResetPassword;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Auth.ResetPassword;

public class ResetPasswordEndpoint(ISender sender) : Endpoint<ResetPasswordRequest>
{
    public override void Configure()
    {
        Post("/api/auth/reset-password");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthStrict));
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        await sender.Send(new ResetPasswordCommand(req.Token, req.NewPassword), ct);
        await Send.OkAsync(cancellation: ct);
    }
}

public record ResetPasswordRequest(string Token, string NewPassword);
