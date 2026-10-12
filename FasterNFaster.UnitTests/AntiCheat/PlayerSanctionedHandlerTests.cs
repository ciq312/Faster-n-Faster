using FasterNFaster.Api.UseCases.AntiCheat.Sanctions;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.AntiCheat;

public class PlayerSanctionedHandlerTests
{
    [Fact]
    public async Task PlayerSanctioned_TerminatesThatPlayersConnections()
    {
        var terminator = new FakeConnectionTerminator();
        var handler = new PlayerSanctionedHandler(terminator);
        var playerId = Guid.NewGuid();

        await handler.Handle(new PlayerSanctionedEvent(playerId), CancellationToken.None);

        Assert.Equal([playerId], terminator.Terminated);
    }
}
