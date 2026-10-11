using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Auth.LoginUser;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Auth.LoginUser;

public class LoginUserEndpoint(ISender sender, IAuthTokenWriter auth) : Endpoint<LoginUserRequest, LoginUserResponse>
{
    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthStrict));
    }

    public override async Task HandleAsync(LoginUserRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new LoginUserCommand(req.Login, req.Password), ct);

        auth.WriteAuth(result.Tokens);

        await Send.OkAsync(new LoginUserResponse(result.UserId, result.Nick), cancellation: ct);
    }
}
