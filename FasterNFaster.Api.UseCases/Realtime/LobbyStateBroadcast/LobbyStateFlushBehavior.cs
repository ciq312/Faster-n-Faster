using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;

public class LobbyStateFlushBehavior<TRequest, TResponse>(ILobbyStateScope scope)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ILobbyStateRequest
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        scope.Run(() => next());
}
