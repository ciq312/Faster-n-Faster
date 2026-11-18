using FasterNFaster.Api.UseCases.AntiCheat.Sanctions;
using FasterNFaster.Api.UseCases.Lobbies.LobbyState;
using FasterNFaster.Api.UseCases.Realtime.PlayerDisconnected;
using FasterNFaster.Api.UseCases.Realtime.PlayerFinished;
using FasterNFaster.Api.UseCases.Realtime.PlayerJoined;
using FasterNFaster.Api.UseCases.Realtime.PlayerKicked;
using FasterNFaster.Api.UseCases.Realtime.PlayerPromoted;
using FasterNFaster.Api.UseCases.Realtime.Ports;
using FasterNFaster.Api.UseCases.Realtime.RaceFinished;
using FasterNFaster.Api.UseCases.Realtime.RaceStarting;
using FasterNFaster.Api.UseCases.Realtime.RaceState;

namespace FasterNFaster.UnitTests.Fakes;

public class RecordingGameClient(Action<string, object?> record) : IGameClient
{
    public Task LobbyState(LobbyStateDTO state) => Record(nameof(LobbyState), state);
    public Task PlayerJoined(PlayerJoinedDTO player) => Record(nameof(PlayerJoined), player);
    public Task PlayerKicked(PlayerKickedDTO player) => Record(nameof(PlayerKicked), player);
    public Task Kicked() => Record(nameof(Kicked));
    public Task PlayerDisconnected(PlayerDisconnectedDTO player) => Record(nameof(PlayerDisconnected), player);
    public Task HostChanged(HostChangedDTO host) => Record(nameof(HostChanged), host);
    public Task RaceStarting(RaceStartingDTO countdown) => Record(nameof(RaceStarting), countdown);
    public Task RaceStarted() => Record(nameof(RaceStarted));
    public Task RaceState(IReadOnlyList<RaceParticipantDTO> participants) => Record(nameof(RaceState), participants);
    public Task PlayerFinished(PlayerFinishedDTO result) => Record(nameof(PlayerFinished), result);
    public Task RaceEnded(RaceEndedDTO results) => Record(nameof(RaceEnded), results);
    public Task RaceWithdrawn() => Record(nameof(RaceWithdrawn));
    public Task Suspended(SuspendedDTO suspension) => Record(nameof(Suspended), suspension);
    public Task Banned(string reason) => Record(nameof(Banned), reason);
    public Task AnotherSessionStarted() => Record(nameof(AnotherSessionStarted));

    private Task Record(string eventName, object? payload = null)
    {
        record(eventName, payload);
        return Task.CompletedTask;
    }
}
