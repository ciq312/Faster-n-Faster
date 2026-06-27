using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class ExternalLoginStore(AppDbContext appDbContext) : IExternalLoginStore
{
    private readonly AppDbContext appDbContext = appDbContext;


    public async Task CreateAsyncLoginInfo(Guid userId, string provider, string subject, string? email)
    {
        var existing = await appDbContext.ExternalLogins.FirstOrDefaultAsync(x => x.ExternalSubject == subject);
        if (existing != null)
            existing.UpdateLogin(email!);


        ExternalLogin externalLogin = new ExternalLogin(userId, subject, email, provider);

        await appDbContext.AddAsync(externalLogin);
        await appDbContext.SaveChangesAsync();
    }
}
