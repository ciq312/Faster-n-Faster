using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;

public class CreateLobbyHandler(ILobbyStore lobbyStore, IPassageProvider passageProvider, ILobbyService lobbyService) : IHandler<CreateLobbyCommand, CreateLobbyResult>
{
    const int DEFAULT_PASSAGE_LENGTH = 50;
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IPassageProvider passageProvider = passageProvider;
    private readonly ILobbyService lobbyService = lobbyService;

    public async Task<CreateLobbyResult> Handle(CreateLobbyCommand command)
    {
        if (lobbyService.IsPlayerInLobby(command.HostId)) throw new InvalidOperationException("Can't create lobby in lobby");

        var passage = await passageProvider.GetPassageAsync(DEFAULT_PASSAGE_LENGTH);

        var race = new WordRace(DEFAULT_PASSAGE_LENGTH);
        race.SetPassage(passage);

        var lobby = new Lobby(command.LobbyName, command.IsPrivate, race);
        lobby.AssignHost(command.HostId);

        var code = await LobbySettings.GenerateUniqueInviteCode(
            c => Task.FromResult(lobbyStore.GetByInviteCode(c) != null));
        lobby.LobbySettings.SetInviteCode(code);


        lobbyStore.Add(lobby);

#if DEBUG
        Log.Information("Created lobby {LobbyId} with host {PlayerId}", lobby.Id, command.HostId);
#endif

        return new CreateLobbyResult(lobby.Id, lobby.Name, lobby.LobbySettings.InviteCode);
    }
}
