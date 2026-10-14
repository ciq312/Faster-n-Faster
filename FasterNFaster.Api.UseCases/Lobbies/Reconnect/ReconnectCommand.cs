using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Reconnect;

public record ReconnectCommand(Guid PlayerId) : IRequest;
