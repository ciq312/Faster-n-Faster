using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public record RefreshCommand(Guid UserId) : IRequest, ILobbyStateRequest;
