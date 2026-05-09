using FastEndpoints;
using FasterNFaster.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<ExternalLogin> ExternalLogins { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<PlayerStatistics> Statistics { get; set; }
    public DbSet<BannedPlayer> BannedPlayers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne(u => u.Statistics)
            .WithOne(s => s.User)
            .HasForeignKey<PlayerStatistics>(s => s.Id);

        modelBuilder.Entity<ExternalLogin>(b =>
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.Provider)
                .HasMaxLength(32)
                .IsRequired();

            b.Property(x => x.ExternalSubject)
                .HasMaxLength(256)
                .IsRequired();

            b.Property(x => x.ExternalEmail)
            .HasMaxLength(256);

            b.HasIndex(x => new { x.Provider, x.ExternalSubject })
                .IsUnique();

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Token>(b =>
        {
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BannedPlayer>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId).IsUnique();
            b.Property(x => x.Reason).HasMaxLength(200);
        });
    }
}