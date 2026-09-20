using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby;

public class CreateLobbyHandler(IPassageProvider passageProvider, ILobbyAccess lobbies, IRaceAccess races) : IRequestHandler<CreateLobbyCommand, CreateLobbyResult>
{
    private const int DefaultPassageLength = 50;

    public async Task<CreateLobbyResult> Handle(CreateLobbyCommand command, CancellationToken cancellationToken)
    {
        var passage = await passageProvider.GetPassageAsync(DefaultPassageLength);

        var lobby = await lobbies.Create(command.LobbyName, command.IsPrivate, command.HostId);

        var race = new WordRace(lobby.Id, DefaultPassageLength);
        race.SetPassage(passage);

        races.Register(race);

        return new CreateLobbyResult(lobby.Id, lobby.LobbySettings.InviteCode);
    }
}
