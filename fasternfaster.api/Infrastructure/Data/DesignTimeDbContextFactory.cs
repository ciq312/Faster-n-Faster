using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FasterNFaster.Api.Infrastructure.Data;

/// <summary>
/// Used by dotnet ef tooling at design time (migrations add, database update, etc.)
/// so it doesn't need to boot the full app or have env vars set.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=postgres;Port=5432;Database=fasterdb;Username=dbuser;Password=dbpassword";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
