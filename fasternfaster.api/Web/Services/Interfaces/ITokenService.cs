namespace FasterNFaster.Api.Web.Services.Interfaces;

public interface ITokenService
{
    public Task HandlePlayerAuth(Guid userId, string userName);

    public Task HandleGuestAuth(Guid guestId, string guestName);

    public Task<bool> TryRefreshToken(string refreshToken);
}