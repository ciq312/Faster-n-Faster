using FasterNFaster.Api.UseCases.AntiCheat.Sanctions;
using FasterNFaster.Api.UseCases.Lobbies.LobbyState;
using FasterNFaster.Api.UseCases.Realtime.PlayerDisconnected;
using FasterNFaster.Api.UseCases.Realtime.PlayerFinished;
using FasterNFaster.Api.UseCases.Realtime.PlayerJoined;
using FasterNFaster.Api.UseCases.Realtime.PlayerKicked;
using FasterNFaster.Api.UseCases.Realtime.PlayerPromoted;
using FasterNFaster.Api.UseCases.Realtime.RaceFinished;
using FasterNFaster.Api.UseCases.Realtime.RaceStarting;
using FasterNFaster.Api.UseCases.Realtime.RaceState;
using TypedSignalR.Client;

namespace FasterNFaster.Api.UseCases.Realtime.Ports;

[Receiver]
public interface IGameClient
{
    Task LobbyState(LobbyStateDTO state);
    Task PlayerJoined(PlayerJoinedDTO player);
    Task PlayerKicked(PlayerKickedDTO player);
    Task Kicked();
    Task PlayerDisconnected(PlayerDisconnectedDTO player);
    Task HostChanged(HostChangedDTO host);
    Task RaceStarting(RaceStartingDTO countdown);
    Task RaceStarted();
    Task RaceState(IReadOnlyList<RaceParticipantDTO> participants);
    Task PlayerFinished(PlayerFinishedDTO result);
    Task RaceEnded(RaceEndedDTO results);
    Task RaceWithdrawn();
    Task Suspended(SuspendedDTO suspension);
    Task Banned(string reason);
    Task AnotherSessionStarted();
}
