using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;

namespace FasterNFaster.Tests.Fakes;

public class FakeExternalLoginRepository : IExternalLoginRepository
{
    public List<ExternalLogin> Logins = new();

    public Task<ExternalLogin?> GetByProviderAndSubjectAsync(string provider, string subject) =>
        Task.FromResult(Logins.FirstOrDefault(x => x.Provider == provider && x.ExternalSubject == subject));

    public Task AddAsync(Guid userId, string provider, string subject, string? email)
    {
        Logins.Add(new ExternalLogin(userId, subject, email, provider));
        return Task.CompletedTask;
    }
}
