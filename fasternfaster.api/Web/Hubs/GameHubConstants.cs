namespace FasterNFaster.Api.Web.Hubs;

public static class GameHubConstants
{
    public static string LobbyGroup(Guid lobbyId) => $"lobby-{lobbyId}";

    public static class Methods
    {
        public const string LobbyState = "LobbyState";
        public const string RaceStarting = "RaceStarting";
        public const string RaceStarted = "RaceStarted";
        public const string RaceState = "RaceState";
        public const string RaceEnded = "RaceEnded";
        public const string PlayerJoined = "PlayerJoined";
        public const string PlayerKicked = "PlayerKicked";
        public const string PlayerDisconnected = "PlayerDisconnected";
        public const string PlayerFinished = "PlayerFinished";
        public const string HostChanged = "HostChanged";
        public const string Kicked = "Kicked";
        public const string Banned = "Banned";
        public const string AnotherSessionStarted = "AnotherSessionStarted";
    }
}
