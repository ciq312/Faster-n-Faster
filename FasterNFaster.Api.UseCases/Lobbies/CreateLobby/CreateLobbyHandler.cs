using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby;

public class CreateLobbyHandler(IPassageProvider passageProvider, ILobbyStore lobbies, IRaceStore races) : IRequestHandler<CreateLobbyCommand, CreateLobbyResult>
{
    private const int DefaultPassageLength = 50;

    public async Task<CreateLobbyResult> Handle(CreateLobbyCommand command, CancellationToken cancellationToken)
    {
        if (lobbies.GetLobbyIdOfPlayer(command.HostId) is not null)
            throw new AlreadyInLobbyException();

        var passage = await passageProvider.GetPassageAsync(DefaultPassageLength);

        var (lobbyId, inviteCode) = await lobbies.Create(command.LobbyName, command.IsPrivate, command.HostId);

        races.Add(new Race(lobbyId, DefaultPassageLength, passage));

        return new CreateLobbyResult(lobbyId, inviteCode);
    }
}
