using FasterNFaster.Api.UseCases.Auth.Ports;

namespace FasterNFaster.Api.Presentation.Web.Auth;

public interface IAuthTokenWriter
{
    void WriteAuth(TokenPair tokens);
    void WriteGuestAuth(IssuedToken accessToken);
    void ClearAuth();
}
