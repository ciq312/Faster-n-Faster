
namespace FasterNFaster.Api.UseCases.Auth.Ports;

public interface IJwtTokenFactory
{
    IssuedToken CreateAccessToken(string userId, string userName);

    IssuedToken CreateRefreshToken();

    IssuedToken CreateGuestAccessToken(string guestId, string guestName);
}
