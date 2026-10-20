using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using FasterNFaster.Api.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.IntegrationTests;

public class UserPersistenceTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    [Fact]
    public async Task SavedUser_CreatedAtStampedByDatabaseAndSurvivesReload()
    {
        var user = new User("alice");
        await App.ExecuteScopedAsync<AppDbContext>(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });
        Assert.NotEqual(default, user.CreatedAt);

        var reloaded = await App.ExecuteScopedAsync<AppDbContext, User>(context =>
            context.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id));

        // Postgres keeps microseconds while ticks are 100 ns.
        Assert.Equal(user.CreatedAt, reloaded.CreatedAt, TimeSpan.FromMicroseconds(1));
    }

    [Fact]
    public async Task SavedUser_IsRegistered()
    {
        var user = new User("bob");
        await App.ExecuteScopedAsync<AppDbContext>(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });

        var registered = await App.ExecuteScopedAsync<AppDbContext, bool>(context =>
            new PostgresUserRepository(context).IsUserRegistered(user.Id));

        Assert.True(registered);
    }

    [Fact]
    public async Task UnknownUserId_IsNotRegistered()
    {
        var registered = await App.ExecuteScopedAsync<AppDbContext, bool>(context =>
            new PostgresUserRepository(context).IsUserRegistered(Guid.NewGuid()));

        Assert.False(registered);
    }
}
