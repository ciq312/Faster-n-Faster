namespace FasterNFaster.Api.UseCases.Shared;

public static class TelemetryOperations
{
    public const string RaceCountdownStarted = "race.countdown_started";
    public const string LobbyCreated = "lobby.created";
    public const string LobbyRemoved = "lobby.removed";
    public const string RaceFinished = "race.finished";
    public const string PlayerKicked = "lobby.player_kicked";
    public const string PassageChanged = "race.passage_changed";
    public const string PlayerJoined = "lobby.player_joined";
    public const string HostTransferred = "lobby.host_transferred";
    public const string PlayerDisconnected = "lobby.player_disconnected";
    public const string UserCredentialsIssued = "auth.credentials_issued";
    public const string UserCredentialsRevoked = "auth.credentials_revoked";
    public const string UserRegisteredAnonymously = "auth.user_registered_anonymously";
}
