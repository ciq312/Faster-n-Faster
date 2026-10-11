using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Auth.RegisterUser;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Auth.RegisterUser;

public class RegisterUserEndpoint(ISender sender) : Endpoint<RegisterUserRequest, RegisterUserResult>
{
    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthStrict));
    }

    public override async Task HandleAsync(RegisterUserRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterUserCommand(req.Nick, req.Login, req.Email, req.Password), ct);
        await Send.CreatedAtAsync<RegisterUserEndpoint>(new { UserID = result.UserId }, result, cancellation: ct);
    }
}
