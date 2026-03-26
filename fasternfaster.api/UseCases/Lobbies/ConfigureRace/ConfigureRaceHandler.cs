using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.ConfigureRace;

public class ConfigureRaceHandler : IHandler<ConfigureRaceCommand>
{
    private readonly ILobbyStore _lobbyStore;

    public ConfigureRaceHandler(ILobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    public Task Handle(ConfigureRaceCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        Race race = command.GameMode.ToLowerInvariant() switch
        {
            "wordcount" => new WordRace(command.WordCount
                ?? throw new ArgumentException("wordCount is required for wordcount mode.")),
            "timer" => new TimerRace(command.TimerDuration
                ?? throw new ArgumentException("timerDuration is required for timer mode.")),
            _ => throw new ArgumentException($"Unknown game mode: {command.GameMode}")
        };

        lobby.ConfigureRace(command.UserId, race);

        return Task.CompletedTask;
    }
}
