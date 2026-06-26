using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.ResetPassword;
using MediatR;

namespace FasterNFaster.Api.Web.Users.ResetPassword;

public class ResetPasswordEndpoint(ISender sender) : Endpoint<ResetPasswordRequest>
{
    public override void Configure()
    {
        Post("/api/auth/reset-password");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        await sender.Send(new ResetPasswordCommand(req.Token, req.NewPassword), ct);
        await Send.OkAsync(cancellation: ct);
    }
}

public record ResetPasswordRequest(string Token, string NewPassword);
