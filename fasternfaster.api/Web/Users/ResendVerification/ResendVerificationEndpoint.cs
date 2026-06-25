using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.ResendVerification;
using MediatR;

namespace FasterNFaster.Api.Web.Users.ResendVerification;

public class ResendVerificationEndpoint(ISender sender) : Endpoint<ResendVerificationRequest>
{
    public override void Configure()
    {
        Post("/api/auth/resend-verification");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ResendVerificationRequest req, CancellationToken ct)
    {
        await sender.Send(new ResendVerificationCommand(req.Email), ct);
        await Send.OkAsync(cancellation: ct);
    }
}

public record ResendVerificationRequest(string Email);
