using FastEndpoints;
using FasterNFaster.Api.Extensions;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Api.Web.Services.Interfaces;
using FasterNFaster.Api.Web.Users.LoginUser.Endpoints;
using MediatR;

namespace FasterNFaster.Api.Web.Users.LoginUser;

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
