using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.UnitTests.Services;

public class UserPersistenceTests
{
    private readonly DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task SavedUser_CreatedAtSurvivesReload()
    {
        var user = new User("alice");
        using (var context = new AppDbContext(options))
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        using var readContext = new AppDbContext(options);
        var reloaded = await readContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);

        Assert.Equal(user.CreatedAt, reloaded.CreatedAt);
    }
}
