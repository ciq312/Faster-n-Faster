namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class LobbyCleanupOptions
{
    public TimeSpan EmptyLobbyTtl { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(1);
}
