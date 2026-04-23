using FastEndpoints;
using FasterNFaster.Api.Infrastructure.Db.Tokens;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.ResetPassword;

namespace FasterNFaster.Api.Web.Users.ResetPassword;

public class ResetPasswordEndpoint(IHandler<ResetPasswordCommand> handler) : Endpoint<ResetPasswordRequest>
{
    private readonly IHandler<ResetPasswordCommand> handler = handler;

    public override void Configure()
    {
        Post("/api/auth/reset-password");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        try
        {
            await handler.Handle(new ResetPasswordCommand(req.Token, req.NewPassword));
            await Send.OkAsync(cancellation: ct);
        }
        catch (TokenNotFoundException)
        {
            // Invalid, expired, or wrong-type tokens all collapse to the same 400
            // so the response can't be used to probe token state.
            ThrowError("This link is invalid or expired", 400);
        }
        catch (UserNotFoundException)
        {
            ThrowError("This link is invalid or expired", 400);
        }
    }
}

public record ResetPasswordRequest(string Token, string NewPassword);
