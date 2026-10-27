using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbyByInviteCode;

public record GetLobbyByInviteCodeQuery(string InviteCode) : IRequest<LobbyListItem>;
