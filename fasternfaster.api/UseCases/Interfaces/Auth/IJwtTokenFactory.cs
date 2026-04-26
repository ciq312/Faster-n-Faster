namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IJwtTokenFactory
{
    string CreateAccessToken(string userId, string userName);

    string CreateRefreshToken();

    string CreateGuestAccessToken(string guestId, string guestName);
}