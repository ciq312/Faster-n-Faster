using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RequestPasswordReset;

namespace FasterNFaster.Api.Web.Users.RequestPasswordReset;

public class RequestPasswordResetEndpoint(IHandler<RequestPasswordResetCommand> handler) : Endpoint<RequestPasswordResetRequest>
{
    private readonly IHandler<RequestPasswordResetCommand> handler = handler;

    public override void Configure()
    {
        Post("/api/auth/forgot-password");
        AllowAnonymous();
    }

    // Always returns 200: unknown email, cooldown hit, and Google-only accounts
    // all no-op silently so callers can't enumerate accounts.
    public override async Task HandleAsync(RequestPasswordResetRequest req, CancellationToken ct)
    {
        await handler.Handle(new RequestPasswordResetCommand(req.Email));
        await Send.OkAsync(cancellation: ct);
    }
}

public record RequestPasswordResetRequest(string Email);
