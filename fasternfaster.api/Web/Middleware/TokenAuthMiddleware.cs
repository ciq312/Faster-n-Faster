using System.Security.Claims;
using FasterNFaster.Api.Infrastructure;

namespace FasterNFaster.Api.Web.Middleware;

public class TokenAuthMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepo)
    {
        var header = context.Request.Headers.Authorization.FirstOrDefault();
        string? token = null;

        if (header != null && header.StartsWith("Bearer "))
            token = header["Bearer ".Length..];

        if (string.IsNullOrEmpty(token) && context.Request.Query.TryGetValue("access_token", out var queryToken))
            token = queryToken.ToString();

        if (!string.IsNullOrEmpty(token))
        {
            var user = await userRepo.GetByTokenAsync(token);

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

        await next(context);
    }
}
