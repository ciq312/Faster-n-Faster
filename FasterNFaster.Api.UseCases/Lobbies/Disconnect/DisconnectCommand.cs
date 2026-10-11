using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public record DisconnectCommand(Guid PlayerId) : IRequest, ILobbyStateRequest;
