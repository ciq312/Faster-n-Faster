using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Ports;
using FasterNFaster.Api.UseCases.Races.Services;
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
        race.ApplyPassage(passage);

        races.Register(race);

        return new CreateLobbyResult(lobby.Id, lobby.LobbySettings.InviteCode);
    }
}
