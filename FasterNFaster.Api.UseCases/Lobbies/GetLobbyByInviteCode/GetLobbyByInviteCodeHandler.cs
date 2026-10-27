using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbyByInviteCode;

public class GetLobbyByInviteCodeHandler(ILobbyStore lobbies, IRaceStore races) : IRequestHandler<GetLobbyByInviteCodeQuery, LobbyListItem>
{
    public async Task<LobbyListItem> Handle(GetLobbyByInviteCodeQuery query, CancellationToken cancellationToken)
    {
        if (lobbies.FindIdByInviteCode(query.InviteCode) is not { } lobbyId || !races.Exists(lobbyId))
            throw new InvalidInviteCodeException();

        return await lobbies.TryRead(lobbyId, LobbyListItem.From)
            ?? throw new InvalidInviteCodeException();
    }
}
