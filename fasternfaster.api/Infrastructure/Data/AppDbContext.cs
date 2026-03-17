using FasterNFaster.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Lobby> Lobbies => Set<Lobby>();
    public DbSet<LobbyPlayer> LobbyPlayers => Set<LobbyPlayer>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();
    public DbSet<CommentThreshold> CommentThresholds => Set<CommentThreshold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lobby>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.InviteCode)
                .IsUnique()
                .HasFilter("\"invite_code\" IS NOT NULL");

            entity.HasMany(e => e.Players)
                .WithOne(p => p.Lobby)
                .HasForeignKey(p => p.LobbyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RaceResults)
                .WithOne(r => r.Lobby)
                .HasForeignKey(r => r.LobbyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LobbyPlayer>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasMany(e => e.RaceResults)
                .WithOne(r => r.LobbyPlayer)
                .HasForeignKey(r => r.LobbyPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<CommentThreshold>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
