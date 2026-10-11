using FastEndpoints;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.UseCases.Users.LoginUser;
using FasterNFaster.Api.Presentation.Web.Services.Interfaces;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Users.LoginUser;

public class LoginUserEndpoint(ISender sender, IAuthTokenWriter auth) : Endpoint<LoginUserRequest, LoginUserResult>
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

        await Send.OkAsync(new LoginUserResult(result.UserId, result.Nick), cancellation: ct);
    }
}
