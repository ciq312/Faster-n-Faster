using FastEndpoints;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Presentation.Extensions;
using FasterNFaster.Api.Presentation.Web.Auth;
using FasterNFaster.Api.UseCases.Users.ChangeNick;
using MediatR;

namespace FasterNFaster.Api.Presentation.Web.Users.ChangeNick;

public class ChangeNickEndpoint(ISender sender, IAuthTokenWriter auth) : Endpoint<ChangeNickRequest>
{
    public override void Configure()
    {
        Put("/api/users/me/nick");
        Roles("Player");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthStrict));
    }

    public override async Task HandleAsync(ChangeNickRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
            throw new UnauthorizedException("Not authorized");

        var result = await sender.Send(new ChangeNickCommand(userId, req.Nick), ct);

        auth.WriteAuth(result.Tokens);

        await Send.OkAsync(cancellation: ct);
    }
}

public record ChangeNickRequest(string Nick);
