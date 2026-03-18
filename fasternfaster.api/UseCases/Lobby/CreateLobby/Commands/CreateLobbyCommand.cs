namespace FasterNFaster.Api.UseCases.Lobby.CreateLobby.Commands;

public record CreateLobbyCommand(
    string? LobbyName,
    string DisplayName,
    string GameMode,
    bool IsPrivate,
    int? WordCount,
    int? TimerDurationSeconds
);
