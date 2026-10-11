using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.IntegrationTests;

public class UserPersistenceTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task SavedUser_CreatedAtSurvivesReload()
    {
        var user = new User("alice");
        await App.ExecuteScopedAsync<AppDbContext>(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });

        var reloaded = await App.ExecuteScopedAsync<AppDbContext, User>(context =>
            context.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id));

        // Postgres keeps microseconds while DateTime ticks are 100 ns.
        Assert.Equal(user.CreatedAt, reloaded.CreatedAt, TimeSpan.FromMicroseconds(1));
    }
}
