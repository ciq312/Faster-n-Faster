using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;
using FasterNFaster.Api.UseCases.Services;

namespace FasterNFaster.Api.UseCases.Lobbies.Refresh;

public class RefreshHandler(IPendingRemovalsRegistry pendingRemovalsRegistry) : IHandler<RefreshCommand>
{
    private readonly IPendingRemovalsRegistry pendingRemovalsRegistry = pendingRemovalsRegistry;
    public async Task Handle(RefreshCommand command)
    {
        if (await pendingRemovalsRegistry.TryCancelPendingRemoval(command.UserId))
            return;
    }
}
