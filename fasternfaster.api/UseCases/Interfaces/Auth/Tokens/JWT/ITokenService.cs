namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface ITokenService
{
    public Task HandlePlayerAuth(Guid userId, string userName);

    public Task HandleGuestAuth(Guid guestId, string guestName);

    public Task<bool> TryRefreshToken(string refreshToken);
}