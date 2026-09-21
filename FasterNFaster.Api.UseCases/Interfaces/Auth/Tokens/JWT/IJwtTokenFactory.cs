
using FasterNFaster.Api.UseCases.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IJwtTokenFactory
{
    IssuedToken CreateAccessToken(string userId, string userName);

    IssuedToken CreateRefreshToken();

    IssuedToken CreateGuestAccessToken(string guestId, string guestName);
}