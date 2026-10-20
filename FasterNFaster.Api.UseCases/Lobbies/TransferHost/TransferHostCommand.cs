using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public record TransferHostCommand(Guid HostId, Guid TargetPlayerId) : IRequest, ILobbyStateRequest;
