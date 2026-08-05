namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IRefreshTokenRepository
{
    Task Issue(Guid userId, string refreshToken, TimeSpan ttl);

    Task<Guid?> RotateRefreshToken(string oldRefreshToken, string newRefreshToken, TimeSpan? ttl);

    Task Invalidate(string refreshToken);

    Task InvalidateAll(Guid userId);
}
