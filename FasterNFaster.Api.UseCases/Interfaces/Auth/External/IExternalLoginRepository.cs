using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetByProviderAndSubjectAsync(string provider, string subject);
    Task AddAsync(Guid userId, string provider, string subject, string? email);
}
