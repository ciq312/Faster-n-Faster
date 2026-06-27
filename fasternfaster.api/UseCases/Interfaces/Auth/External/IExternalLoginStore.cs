namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IExternalLoginStore
{
    Task CreateAsyncLoginInfo(Guid userId, string provider, string subject, string? email);
}
