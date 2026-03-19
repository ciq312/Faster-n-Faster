using System.Security.Claims;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.Web.Middleware;

public class TokenAuthMiddleware
{
    private readonly RequestDelegate _next;

    public TokenAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepo)
    {
        var header = context.Request.Headers.Authorization.FirstOrDefault();
        string? token = null;

        if (header != null && header.StartsWith("Bearer "))
            token = header["Bearer ".Length..];

        // SignalR sends token via query string during WebSocket upgrade
        if (string.IsNullOrEmpty(token) && context.Request.Query.TryGetValue("access_token", out var queryToken))
            token = queryToken.ToString();

        if (!string.IsNullOrEmpty(token))
        {
            var user = userRepo.GetByToken(token);

            if (user != null)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Nick)
                };
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "Token"));
            }
        }

        await _next(context);
    }
}
