using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public record DisconnectCommand(Guid PlayerId) : IRequest, ILobbyStateRequest;
