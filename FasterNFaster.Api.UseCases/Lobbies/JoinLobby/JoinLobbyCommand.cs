using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public record JoinLobbyCommand(Guid PlayerId, Guid LobbyId, string Nick, string? InviteCode = null) : IRequest, ILobbyStateRequest;
