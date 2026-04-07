using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;

public class CreateLobbyHandler : IHandler<CreateLobbyCommand, CreateLobbyResult>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IPassageProvider _passageProvider;

    public CreateLobbyHandler(ILobbyStore lobbyStore, IPassageProvider passageProvider)
    {
        _lobbyStore = lobbyStore;
        _passageProvider = passageProvider;
    }

    public async Task<CreateLobbyResult> Handle(CreateLobbyCommand command)
    {
        var lobby = new Lobby(command.LobbyName, command.IsPrivate);
        lobby.AssignHost(command.HostId);

        if (command.IsPrivate)
        {
            var code = await LobbySettings.GenerateUniqueInviteCode(
                c => Task.FromResult(_lobbyStore.GetByInviteCode(c) != null));
            lobby.LobbySettings.SetInviteCode(code);
        }

        var passage = await _passageProvider.GetPassageAsync(lobby.RaceSettings.WordCount);
        lobby.SetInitialPassage(passage);

        _lobbyStore.Add(lobby);

        Log.Information("Created lobby {LobbyId} with host {PlayerId}", lobby.Id, command.HostId);

        return new CreateLobbyResult(lobby.Id, lobby.Name, lobby.LobbySettings.InviteCode);
    }
}
