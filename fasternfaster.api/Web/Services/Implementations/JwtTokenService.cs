using System.Collections.Concurrent;
using System.Security.Cryptography;
using FastEndpoints.Security;
using Microsoft.IdentityModel.Tokens;

namespace FasterNFaster.Api.Web.Services;

public class JwtTokenService(IConfiguration config)
{
    private readonly IConfiguration config = config;

    private static readonly ConcurrentDictionary<string, RefreshTokenData> refreshTokens = new();
    private static readonly ConcurrentDictionary<string, string> userNames = new();


    public (string accessToken, string refreshToken) CreateTokenPair(TokenCreationRequest request)
    {
        RevokeAllForUser(request.UserId.ToString());

        string accessToken = CreateAccessToken(request, TimeSpan.FromMinutes(5), "Player");
        string refreshToken = GenerateRefreshToken();

        userNames[request.UserId.ToString()] = request.UserName;
        refreshTokens[refreshToken] = new RefreshTokenData
        {
            UserId = request.UserId.ToString(),
            Expiry = DateTime.UtcNow.AddHours(4)
        };

        return (accessToken, refreshToken);
    }

    private void SetTokenCookies(HttpResponse response, string tokenType, string token, TimeSpan expiry, string path = "/")
    {
        response.Cookies.Append(tokenType, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // TODO: set true in production
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.Add(expiry),
            Path = path,
        });
    }
    public void SetAccessAndRefreshTokenCookies(HttpResponse response, string accessToken, string refreshToken)
    {
        SetTokenCookies(response, "access_token", accessToken, TimeSpan.FromMinutes(5));
        SetTokenCookies(response, "refresh_token", refreshToken, TimeSpan.FromHours(4), "/api/auth/refresh");
    }

    public void SetAccessTokenGuestCookie(HttpResponse response, string accessToken)
    {
        SetTokenCookies(response, "access_token", accessToken, TimeSpan.FromDays(7));
    }

    public void ClearTokenCookies(HttpResponse response)
    {
        response.Cookies.Delete("access_token");
        response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth/refresh" });
    }

    /// <summary>
    /// Validates the refresh token from cookie, rotates it, returns new pair.
    /// Returns null if invalid/expired (caller should return 401).
    /// </summary>
    public (string accessToken, string refreshToken)? TryRefresh(string refreshToken)
    {
        if (!refreshTokens.TryGetValue(refreshToken, out var data))
            return null;

        if (data.Expiry < DateTime.UtcNow)
        {
            refreshTokens.TryRemove(refreshToken, out _);
            return null;
        }

        if (!userNames.TryGetValue(data.UserId, out var userName))
            return null;

        return CreateTokenPair(new TokenCreationRequest
        {
            UserId = Guid.Parse(data.UserId),
            UserName = userName
        });
    }


    public void Revoke(string refreshToken)
    {
        if (refreshTokens.TryRemove(refreshToken, out var data))
            RevokeAllForUser(data.UserId);
    }

    public void RevokeAllForUser(string userId)
    {
        var userTokens = refreshTokens
            .Where(kvp => kvp.Value.UserId == userId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in userTokens)
            refreshTokens.TryRemove(token, out _);
    }

    public string CreateGuestAccessToken(TokenCreationRequest request)
    {
        return CreateAccessToken(request, TimeSpan.FromDays(7), "Guest");
    }

    private string CreateAccessToken(TokenCreationRequest request, TimeSpan expiry, string role)
    {
        return JwtBearer.CreateToken(o =>
        {
            o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
            o.SigningStyle = TokenSigningStyle.Asymmetric;
            o.SigningKey = config["JwtOptions:JWT_PRIVATE_TOKEN"]!;
            o.ExpireAt = DateTime.UtcNow.Add(expiry);
            o.Issuer = config["JwtOptions:Issuer"];
            o.Audience = config["JwtOptions:Audience"];
            o.User.Roles.Add(role);
            o.User.Claims.Add(("UserId", request.UserId.ToString()));
            o.User.Claims.Add(("UserName", request.UserName));
        });
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private class RefreshTokenData
    {
        public required string UserId { get; init; }
        public DateTime Expiry { get; init; }
    }
}

public class TokenCreationRequest
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

}
