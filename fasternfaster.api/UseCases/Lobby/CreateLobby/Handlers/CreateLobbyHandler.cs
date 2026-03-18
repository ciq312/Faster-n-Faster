using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobby.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobby.CreateLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobby.CreateLobby.Handlers;

public class CreateLobbyHandler : IHandler<CreateLobbyCommand, CreateLobbyResult>
{
    private readonly ILobbyRepository _repository;

    public CreateLobbyHandler(ILobbyRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateLobbyResult> Handle(CreateLobbyCommand command)
    {
        // var defaultName = string.IsNullOrWhiteSpace(command.LobbyName)
        //     ? $"{command.DisplayName}'s Lobby"
        //     : command.LobbyName;

        var lobby = await Core.Entities.Lobby.Create(
            command.LobbyName,
            command.IsPrivate,
            nameExists: _repository.NameExistsAsync,
            inviteCodeExists: _repository.InviteCodeExistsAsync
        );

        switch (command.GameMode.ToLowerInvariant())
        {
            case "wordcount":
                lobby.ConfigureWordRace(
                    command.WordCount
                        ?? throw new ArgumentException("WordCount is required for word count mode.")
                );
                break;
            case "timer":
                lobby.ConfigureTimerRace(
                    command.TimerDurationSeconds
                        ?? throw new ArgumentException(
                            "TimerDurationSeconds is required for timer mode."
                        )
                );
                break;
            default:
                throw new ArgumentException($"Unknown game mode: '{command.GameMode}'.");
        }

        await _repository.AddAsync(lobby);

        return new CreateLobbyResult(lobby.Id, lobby.Name);
    }
}
