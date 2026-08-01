using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.HostChanged;
using FasterNFaster.Api.UseCases.Realtime.PlayerDisconnected;
using FasterNFaster.Api.UseCases.Realtime.PlayerFinished;
using FasterNFaster.Api.UseCases.Realtime.PlayerJoined;
using FasterNFaster.Api.UseCases.Realtime.PlayerKicked;
using FasterNFaster.Api.UseCases.Realtime.RaceFinished;
using FasterNFaster.Api.UseCases.Realtime.RaceStarting;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Handlers;

public class RealtimeBroadcastHandlerTests
{
    [Fact]
    public async Task PlayerKicked_NotifiesLobbyAndPlayer_RefreshesState()
    {
        var lobbyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var broadcaster = new FakeBroadcaster();
        var state = new FakeLobbyStateBroadcaster();
        var handler = new BroadcastPlayerKickedHandler(broadcaster, state);

        await handler.Handle(
            new DomainEventNotification<PlayerKickedEvent>(new PlayerKickedEvent(userId, lobbyId, "nick")),
            CancellationToken.None);

        Assert.Equal(2, broadcaster.Broadcasts.Count);

        var notice = broadcaster.Broadcasts[0];
        Assert.Equal(GameEvents.PlayerKicked, notice.EventName);
        var noticeAudience = Assert.IsType<LobbyAudience>(notice.Audience);
        Assert.Equal(lobbyId, noticeAudience.LobbyId);
        var payload = Assert.IsType<PlayerKickedDTO>(notice.Payload);
        Assert.Equal(userId, payload.UserId);
        Assert.Equal("nick", payload.Nick);

        var personal = broadcaster.Broadcasts[1];
        Assert.Equal(GameEvents.Kicked, personal.EventName);
        var personalAudience = Assert.IsType<PlayerAudience>(personal.Audience);
        Assert.Equal(userId, personalAudience.UserId);
        Assert.Null(personal.Payload);

        Assert.Equal(lobbyId, Assert.Single(state.ByLobbyId));
    }

    [Fact]
    public async Task PlayerDisconnected_BroadcastsToLobby_RefreshesState()
    {
        var lobbyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var broadcaster = new FakeBroadcaster();
        var state = new FakeLobbyStateBroadcaster();
        var handler = new BroadcastPlayerDisconnectedHandler(broadcaster, state);

        await handler.Handle(
            new DomainEventNotification<PlayerDisconnectedEvent>(new PlayerDisconnectedEvent(userId, lobbyId, "nick")),
            CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.PlayerDisconnected, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(lobbyId, audience.LobbyId);
        var payload = Assert.IsType<PlayerDisconnectedDTO>(sent.Payload);
        Assert.Equal(userId, payload.DisconnectedUserId);
        Assert.Equal("nick", payload.DisconnectedUserNick);

        Assert.Equal(lobbyId, Assert.Single(state.ByLobbyId));
    }

    [Fact]
    public async Task HostChanged_BroadcastsToLobby_RefreshesState()
    {
        var lobbyId = Guid.NewGuid();
        var newHostId = Guid.NewGuid();
        var broadcaster = new FakeBroadcaster();
        var state = new FakeLobbyStateBroadcaster();
        var handler = new BroadcastPlayerPromotedHandler(broadcaster, state);

        await handler.Handle(
            new DomainEventNotification<HostChangedEvent>(new HostChangedEvent(lobbyId, newHostId, "newhost")),
            CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.HostChanged, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(lobbyId, audience.LobbyId);
        var payload = Assert.IsType<HostChangedDTO>(sent.Payload);
        Assert.Equal(newHostId, payload.UserId);
        Assert.Equal("newhost", payload.NewHostNick);

        Assert.Equal(lobbyId, Assert.Single(state.ByLobbyId));
    }

    [Fact]
    public async Task PlayerFinished_BroadcastsToLobby()
    {
        var lobbyId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastPlayerFinishedHandler(broadcaster);

        var e = new PlayerFinishedEvent("nick", playerId, 1, 80.0, 95.0);
        e.WrapRaceContext(lobbyId);

        await handler.Handle(new DomainEventNotification<PlayerFinishedEvent>(e), CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.PlayerFinished, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(lobbyId, audience.LobbyId);
        var payload = Assert.IsType<PlayerFinishedDTO>(sent.Payload);
        Assert.Equal("nick", payload.Nick);
        Assert.Equal(playerId, payload.PlayerId);
        Assert.Equal(1, payload.FinishPosition);
        Assert.Equal(80.0, payload.Wpm);
        Assert.Equal(95.0, payload.Accuracy);
    }

    [Fact]
    public async Task RaceFinished_BroadcastsResults_RefreshesStateWithLobby()
    {
        var lobby = new Lobby("test", false);
        var results = new List<RaceParticipantResult>();
        var broadcaster = new FakeBroadcaster();
        var state = new FakeLobbyStateBroadcaster();
        var handler = new BroadcastRaceFinishedHandler(broadcaster, state);

        await handler.Handle(new RaceSessionEndedEvent(lobby, results), CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.RaceEnded, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(lobby.Id, audience.LobbyId);
        var payload = Assert.IsType<RaceEndedDTO>(sent.Payload);
        Assert.Same(results, payload.Results);

        Assert.Same(lobby, Assert.Single(state.ByLobby));
    }

    [Fact]
    public async Task PlayerJoined_RefreshesState_NotifiesOthers()
    {
        var lobbyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var broadcaster = new FakeBroadcaster();
        var state = new FakeLobbyStateBroadcaster();
        var handler = new BroadcastPlayerJoinedHandler(broadcaster, state);

        await handler.Handle(
            new DomainEventNotification<PlayerJoinedEvent>(new PlayerJoinedEvent(userId, lobbyId, "nick")),
            CancellationToken.None);

        Assert.Equal(lobbyId, Assert.Single(state.ByLobbyId));

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.PlayerJoined, sent.EventName);
        var audience = Assert.IsType<LobbyExceptAudience>(sent.Audience);
        Assert.Equal(lobbyId, audience.LobbyId);
        Assert.Equal(userId, audience.UserId);
        var payload = Assert.IsType<PlayerJoinedDTO>(sent.Payload);
        Assert.Equal(userId, payload.PlayerId);
        Assert.Equal("nick", payload.DisplayName);
    }

    [Fact]
    public async Task RaceStarting_BroadcastsCountdownToLobby()
    {
        var lobbyId = Guid.NewGuid();
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastRaceStartingHandler(broadcaster);

        await handler.Handle(
            new DomainEventNotification<SessionStartedEvent>(new SessionStartedEvent(lobbyId)),
            CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.RaceStarting, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(lobbyId, audience.LobbyId);
        var payload = Assert.IsType<RaceStartingDTO>(sent.Payload);
        Assert.Equal(3, payload.CountdownSeconds);
    }
}
