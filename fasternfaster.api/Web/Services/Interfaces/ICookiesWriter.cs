namespace FasterNFaster.Api.Web.Services.Interfaces;

public interface ICookiesWriter
{
    void WriteRefreshTokenCookie(string token);
    void WriteAccessTokenCookie(string token);
    void WriteGuestAccessTokenCookie(string token);
    void DeleteTokensCookies();
}