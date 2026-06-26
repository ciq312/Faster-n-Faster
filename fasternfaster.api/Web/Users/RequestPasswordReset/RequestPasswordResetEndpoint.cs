using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.RequestPasswordReset;
using MediatR;

namespace FasterNFaster.Api.Web.Users.RequestPasswordReset;

public class RequestPasswordResetEndpoint(ISender sender) : Endpoint<RequestPasswordResetRequest>
{
    public override void Configure()
    {
        Post("/api/auth/forgot-password");
        AllowAnonymous();
    }

    // Always returns 200: unknown email, cooldown hit, and Google-only accounts
    // all no-op silently so callers can't enumerate accounts.
    public override async Task HandleAsync(RequestPasswordResetRequest req, CancellationToken ct)
    {
        await sender.Send(new RequestPasswordResetCommand(req.Email), ct);
        await Send.OkAsync(cancellation: ct);
    }
}

public record RequestPasswordResetRequest(string Email);
