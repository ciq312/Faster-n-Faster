using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetByProviderAndSubjectAsync(string provider, string subject);
    void Add(Guid userId, string provider, string subject, string? email);
}
