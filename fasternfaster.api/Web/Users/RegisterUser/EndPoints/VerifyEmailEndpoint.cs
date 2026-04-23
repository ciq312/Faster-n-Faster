using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.VerifyEmail;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace FasterNFaster.Api.Web.Users.RegisterUser.EndPoints;

public class VerifyEmailEndpoint(IHandler<VerifyEmailCommand> handler) : Endpoint<VerifyEmailRequest>
{
    private readonly IHandler<VerifyEmailCommand> handler = handler;

    public override void Configure()
    {
        Post("/api/auth/verify-email");
        AllowAnonymous();
    }

    public override async Task HandleAsync(VerifyEmailRequest req, CancellationToken ct)
    {
        await handler.Handle(new VerifyEmailCommand(req.Token));

        await Send.OkAsync(cancellation: ct);
    }
}
public record VerifyEmailRequest(string Token);
