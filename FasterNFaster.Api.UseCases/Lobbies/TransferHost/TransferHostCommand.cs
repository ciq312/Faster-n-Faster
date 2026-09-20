using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public record TransferHostCommand(Guid HostId, Guid TargetPlayerId) : IRequest, ILobbyStateRequest;
