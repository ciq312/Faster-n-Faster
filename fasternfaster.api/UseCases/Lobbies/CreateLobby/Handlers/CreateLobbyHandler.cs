using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;

public class CreateLobbyHandler : IHandler<CreateLobbyCommand, CreateLobbyResult>
{
    private readonly ILobbyStore _lobbyStore;

    public CreateLobbyHandler(ILobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    public Task<CreateLobbyResult> Handle(CreateLobbyCommand command)
    {
        var lobby = new Lobby(command.LobbyName, command.IsPrivate);

        switch (command.GameMode.ToLowerInvariant())
        {
            case "wordcount":
                lobby.ConfigureWordRace(command.WordCount!.Value);
                break;
            case "timer":
                lobby.ConfigureTimerRace(command.TimerDurationSeconds!.Value);
                break;
        }

        lobby.AssignHost(command.HostId);

        _lobbyStore.Add(lobby);

        Log.Information("Created lobby {LobbyId} with host {PlayerId}", lobby.Id);

        return Task.FromResult(new CreateLobbyResult(lobby.Id, lobby.Name));
    }
}
