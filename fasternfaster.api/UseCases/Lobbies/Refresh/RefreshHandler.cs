using FasterNFaster.Api.UseCases.Interfaces.Users;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public class RefreshHandler(IPendingRemovalsRegistry pendingRemovalsRegistry) : IRequestHandler<RefreshCommand>
{
    public async Task Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        await pendingRemovalsRegistry.TryCancelPendingRemoval(command.UserId);
    }
}
