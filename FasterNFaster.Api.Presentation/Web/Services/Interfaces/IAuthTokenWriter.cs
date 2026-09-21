using FasterNFaster.Api.UseCases.Auth;

namespace FasterNFaster.Api.Web.Services.Interfaces;

public interface IAuthTokenWriter
{
    void WriteAuth(TokenPair tokens);
    void WriteGuestAuth(IssuedToken accessToken);
    void ClearAuth();
}
