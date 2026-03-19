using NJsonSchema.Validation.FormatValidators;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;

public record CreateLobbyCommand(
    string LobbyName,
    string DisplayName,
    string GameMode,
    bool IsPrivate,
    int? WordCount,
    int? TimerDurationSeconds,
    Guid HostId
);
