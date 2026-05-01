using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface ITokenStore
{
    Task StoreRefreshToken(Guid userId, string refreshToken);
    public Task<bool> TryRefreshToken(string oldRefreshToken, string newRefreshToken);
    Task<bool> IsRefreshTokenValid(string refreshToken);
    Task DeleteRefreshToken(string refreshToken);
    public Task<Guid> GetUserIdByTokenAsync(string refreshToken);
    Task DeleteAllTokensForUserAsync(Guid userId);
}