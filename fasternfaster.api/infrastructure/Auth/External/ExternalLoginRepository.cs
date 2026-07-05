using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.Infrastructure.Db;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Auth;

public class ExternalLoginRepository(AppDbContext appDbContext) : IExternalLoginRepository
{
    public Task<ExternalLogin?> GetByProviderAndSubjectAsync(string provider, string subject) =>
        appDbContext.ExternalLogins.FirstOrDefaultAsync(x => x.Provider == provider && x.ExternalSubject == subject);

    public async Task AddAsync(Guid userId, string provider, string subject, string? email)
    {
        appDbContext.ExternalLogins.Add(new ExternalLogin(userId, subject, email, provider));
        await appDbContext.SaveChangesAsync();
    }
}
