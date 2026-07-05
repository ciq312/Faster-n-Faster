using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.FastReconnect;

public record FastReconnectCommand(Guid LobbyId, Guid PlayerId) : IRequest;
