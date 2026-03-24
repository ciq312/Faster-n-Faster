using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobby;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;

public class CreateLobbyHandler : IHandler<CreateLobbyCommand, CreateLobbyResult>
{
    const int DEFAULT_WORDS_NUM = 50;
    private readonly ILobbyStore _lobbyStore;

    public CreateLobbyHandler(ILobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    public Task<CreateLobbyResult> Handle(CreateLobbyCommand command)
    {
        var lobby = new Lobby(command.LobbyName, command.IsPrivate);

        Race race = new WordRace(DEFAULT_WORDS_NUM);

        lobby.ConfigureRace(race);

        lobby.AssignHost(command.HostId);

        _lobbyStore.Add(lobby);

        Log.Information("Created lobby {LobbyId} with host {PlayerId}", lobby.Id, command.HostId);

        return Task.FromResult(new CreateLobbyResult(lobby.Id, lobby.Name));
    }
}
