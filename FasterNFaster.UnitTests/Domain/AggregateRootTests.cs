using FasterNFaster.Api.Domain.Entities.Lobbies;
using FasterNFaster.Api.Domain.Entities.Lobbies.Events;

namespace FasterNFaster.UnitTests.Domain;

public class AggregateRootTests
{
    [Fact]
    public void DrainEvents_WhenNoEvents_ReturnsSameEmptyInstance()
    {
        var lobby = new Lobby("Test", isPrivate: false, inviteCodeExists: _ => false);

        var first = lobby.DrainEvents();
        var second = lobby.DrainEvents();

        Assert.Empty(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void DrainEvents_AfterEvents_ReturnsThemAndClears()
    {
        var lobby = new Lobby("Test", isPrivate: false, inviteCodeExists: _ => false);
        var playerId = Guid.NewGuid();
        lobby.Join(playerId, "player", null, _ => true);

        var drained = lobby.DrainEvents();

        var joined = Assert.IsType<PlayerJoinedEvent>(Assert.Single(drained));
        Assert.Equal(playerId, joined.UserId);
        Assert.Empty(lobby.DomainEvents);
        Assert.Empty(lobby.DrainEvents());
    }
}
