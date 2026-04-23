using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.ResendVerification;

namespace FasterNFaster.Api.Web.Users.ResendVerification;

public class ResendVerificationEndpoint(IHandler<ResendVerificationCommand> handler) : Endpoint<ResendVerificationRequest>
{
    private readonly IHandler<ResendVerificationCommand> handler = handler;

    public override void Configure()
    {
        Post("/api/auth/resend-verification");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ResendVerificationRequest req, CancellationToken ct)
    {
        await handler.Handle(new ResendVerificationCommand(req.Email));

        await Send.OkAsync(cancellation: ct);
    }
}

public record ResendVerificationRequest(string Email);
