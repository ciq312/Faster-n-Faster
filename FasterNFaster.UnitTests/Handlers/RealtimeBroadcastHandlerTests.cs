using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Entities.Lobbies.Events;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Core.Entities.Races.Events;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.Infrastructure.Users;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.UseCases.Interfaces.Realtime;
using FasterNFaster.Api.UseCases.Realtime;
using FasterNFaster.Api.UseCases.Realtime.HostChanged;
using FasterNFaster.Api.UseCases.Realtime.PlayerDisconnected;
using FasterNFaster.Api.UseCases.Realtime.PlayerFinished;
using FasterNFaster.Api.UseCases.Realtime.PlayerJoined;
using FasterNFaster.Api.UseCases.Realtime.PlayerKicked;
using FasterNFaster.Api.UseCases.Realtime.RaceFinished;
using FasterNFaster.Api.UseCases.Realtime.RaceStarting;
using FasterNFaster.Api.UseCases.Services;
using FasterNFaster.Api.UseCases.Services.Races;
using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Handlers;

public class RealtimeBroadcastHandlerTests
{
    [Fact]
    public async Task PlayerKicked_NotifiesLobbyAndPlayer()
    {
        var user = new User("test");
        var userId = user.Id;
        var context = await LobbyFactory.WithPlayers(user, new User("test1"));
        var lobbyId = context.LobbyId;
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastPlayerKickedHandler(broadcaster);

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
    }

    [Fact]
    public async Task PlayerDisconnected_BroadcastsToLobby()
    {
        var user = new User("test");
        var userId = user.Id;
        var context = await LobbyFactory.WithPlayers(user);
        var lobbyId = context.LobbyId;
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastPlayerDisconnectedHandler(broadcaster, context.LobbyAccess);

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
    }

    [Fact]
    public async Task HostChanged_BroadcastsToLobby()
    {
        var user = new User("test");
        var userId = user.Id;
        var newHost = new User("newhost");
        var newHostId = newHost.Id;
        var context = await LobbyFactory.WithPlayers(user);
        var lobbyId = context.LobbyId;
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastPlayerPromotedHandler(broadcaster);

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
    }

    [Fact]
    public async Task PlayerFinished_BroadcastsToLobby()
    {
        var user = new User("test");
        var userId = user.Id;
        var context = await LobbyFactory.WithPlayers(user);
        var lobbyId = context.LobbyId;
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastPlayerFinishedHandler(broadcaster);

        var e = new PlayerFinishedEvent(lobbyId, "nick", userId, 1, 80.0, 95.0);

        await handler.Handle(new DomainEventNotification<PlayerFinishedEvent>(e), CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.PlayerFinished, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(lobbyId, audience.LobbyId);
        var payload = Assert.IsType<PlayerFinishedDTO>(sent.Payload);
        Assert.Equal("nick", payload.Nick);
        Assert.Equal(userId, payload.PlayerId);
        Assert.Equal(1, payload.FinishPosition);
        Assert.Equal(80.0, payload.Wpm);
        Assert.Equal(95.0, payload.Accuracy);
    }

    [Fact]
    public async Task RaceFinished_BroadcastsResults()
    {

        var user = new User("test");
        var context = await LobbyFactory.WithPlayers(user);
        var playerId = Guid.NewGuid();
        var results = new List<RaceParticipantResult>
        {
            new(Guid.NewGuid(), playerId, "nick", 80f, 95f, 3, 50, 1)
        };
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastRaceFinishedHandler(broadcaster);

        var e = new RaceFinishedEvent(context.Lobby.Id, results);

        await handler.Handle(new DomainEventNotification<RaceFinishedEvent>(e), CancellationToken.None);

        var sent = Assert.Single(broadcaster.Broadcasts);
        Assert.Equal(GameEvents.RaceEnded, sent.EventName);
        var audience = Assert.IsType<LobbyAudience>(sent.Audience);
        Assert.Equal(context.Lobby.Id, audience.LobbyId);
        var payload = Assert.IsType<RaceEndedDTO>(sent.Payload);
        var result = Assert.Single(payload.Results);
        Assert.Equal(new RaceResultDTO(playerId, "nick", 1, 80f, 95f, 3), result);
    }

    [Fact]
    public async Task PlayerJoined_NotifiesOthers()
    {
        var user = new User("test");
        var userId = user.Id;
        var context = await LobbyFactory.WithPlayers(user);
        var lobbyId = context.LobbyId;
        var broadcaster = new FakeBroadcaster();
        var handler = new BroadcastPlayerJoinedHandler(broadcaster);

        await handler.Handle(
            new DomainEventNotification<PlayerJoinedEvent>(new PlayerJoinedEvent(userId, lobbyId, "nick")),
            CancellationToken.None);

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
        var user = new User("test");
        var context = await LobbyFactory.WithPlayers(user);
        var lobbyId = context.LobbyId;
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
