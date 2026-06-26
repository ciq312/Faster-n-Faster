using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Handlers;

public class CreateLobbyHandler(IPassageProvider passageProvider, ILobbyService lobbyService, IRaceService raceService) : IRequestHandler<CreateLobbyCommand, CreateLobbyResult>
{
    private const int DefaultPassageLength = 50;

    public async Task<CreateLobbyResult> Handle(CreateLobbyCommand command, CancellationToken cancellationToken)
    {
        var passage = await passageProvider.GetPassageAsync(DefaultPassageLength);

        var race = new WordRace(DefaultPassageLength);
        race.SetPassage(passage);

        var lobby = await lobbyService.CreateLobby(command.LobbyName, command.IsPrivate, command.HostId);

        raceService.RegisterRace(lobby.Id, race);

        return new CreateLobbyResult(lobby.Id, lobby.LobbySettings.InviteCode);
    }
}
