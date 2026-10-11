namespace FasterNFaster.Api.UseCases.Auth.Ports;

public interface IExternalLoginRepository
{
    Task<Domain.Entities.Auth.ExternalLogin?> GetByProviderAndSubjectAsync(string provider, string subject);
    void Add(Guid userId, string provider, string subject, string? email);
}
