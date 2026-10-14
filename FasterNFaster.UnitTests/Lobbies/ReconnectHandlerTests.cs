using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Reconnect;

namespace FasterNFaster.UnitTests.Lobbies;

public class ReconnectHandlerTests
{
    private readonly PendingRemovalRegistry registry = new();

    private Task Reconnect(Guid playerId) =>
        new ReconnectHandler(registry).Handle(new ReconnectCommand(playerId), CancellationToken.None);

    [Fact]
    public async Task PendingRemoval_IsCancelled()
    {
        var playerId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        registry.StorePendingRemoval(playerId, cts);

        await Reconnect(playerId);

        Assert.True(cts.IsCancellationRequested);
        Assert.False(registry.TryCancelPendingRemoval(playerId));
    }
}
