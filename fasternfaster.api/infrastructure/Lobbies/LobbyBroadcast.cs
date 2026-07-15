namespace FasterNFaster.Api.Infrastructure.Lobbies;

public partial class RaceStateConflator
{
    private sealed class LobbyBroadcast
    {
        public RaceFrame? Latest;
        public int Running;
    }
}
