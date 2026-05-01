using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Infrastructure;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;

public class CreateLobbyHandler(IPassageProvider passageProvider, ILobbyService lobbyService) : IHandler<CreateLobbyCommand, CreateLobbyResult>
{
    const int DEFAULT_PASSAGE_LENGTH = 50;
    private readonly IPassageProvider passageProvider = passageProvider;
    private readonly ILobbyService lobbyService = lobbyService;

    public async Task<CreateLobbyResult> Handle(CreateLobbyCommand command)
    {
        var passage = await passageProvider.GetPassageAsync(DEFAULT_PASSAGE_LENGTH);

        var race = new WordRace(DEFAULT_PASSAGE_LENGTH);
        race.SetPassage(passage);

        Lobby lobby = await lobbyService.CreateLobby(command.LobbyName, command.IsPrivate, race, command.HostId);

#if DEBUG
        Log.Information("Created lobby {LobbyId} with host {PlayerId}", lobby.Id, command.HostId);
#endif

        return new CreateLobbyResult(lobby.Id, lobby.LobbySettings.InviteCode);
    }
}
