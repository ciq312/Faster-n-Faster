using FasterNFaster.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users { get; set; }

    public DbSet<PlayerStatistics> Statistics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne(u => u.Statistics)
            .WithOne(s => s.User)
            .HasForeignKey<PlayerStatistics>(s => s.Id);
    }
}