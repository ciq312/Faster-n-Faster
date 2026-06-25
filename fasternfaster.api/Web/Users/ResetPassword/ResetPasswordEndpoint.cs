using FastEndpoints;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Exceptions;
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
        try
        {
            await sender.Send(new ResetPasswordCommand(req.Token, req.NewPassword), ct);
            await Send.OkAsync(cancellation: ct);
        }
        catch (TokenNotFoundException)
        {
            ThrowError("This link is invalid or expired", 400);
        }
        catch (UserNotFoundException)
        {
            ThrowError("This link is invalid or expired", 400);
        }
    }
}

public record ResetPasswordRequest(string Token, string NewPassword);
