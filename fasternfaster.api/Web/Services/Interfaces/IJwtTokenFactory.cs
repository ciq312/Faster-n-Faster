namespace FasterNFaster.Api.Web.Services.Interfaces;

public interface IJwtTokenFactory
{
    string CreateAccessToken(string userId, string userName);

    string CreateRefreshToken();

    string CreateGuestAccessToken(string guestId, string guestName);
}