import { createAction, type Dispatch } from "@reduxjs/toolkit";
import type { SuspendedDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.AntiCheat.Sanctions";
import type { LobbyStateDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Lobbies.LobbyState";
import type { PlayerDisconnectedDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.PlayerDisconnected";
import type { PlayerFinishedDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.PlayerFinished";
import type { PlayerJoinedDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.PlayerJoined";
import type { PlayerKickedDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.PlayerKicked";
import type { HostChangedDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.PlayerPromoted";
import type { RaceEndedDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceFinished";
import type { RaceStartingDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceStarting";
import type { RaceParticipantDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceState";
import type { IGameClient } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.UseCases.Realtime.Ports";

export const hubEvents = {
  lobbyState: createAction<LobbyStateDTO>("hubEvent/lobbyState"),
  playerJoined: createAction<PlayerJoinedDTO>("hubEvent/playerJoined"),
  playerKicked: createAction<PlayerKickedDTO>("hubEvent/playerKicked"),
  kicked: createAction("hubEvent/kicked"),
  playerDisconnected: createAction<PlayerDisconnectedDTO>(
    "hubEvent/playerDisconnected",
  ),
  hostChanged: createAction<HostChangedDTO>("hubEvent/hostChanged"),
  raceStarting: createAction<RaceStartingDTO>("hubEvent/raceStarting"),
  raceStarted: createAction("hubEvent/raceStarted"),
  raceState: createAction<RaceParticipantDTO[]>("hubEvent/raceState"),
  playerFinished: createAction<PlayerFinishedDTO>("hubEvent/playerFinished"),
  raceEnded: createAction<RaceEndedDTO>("hubEvent/raceEnded"),
  raceWithdrawn: createAction("hubEvent/raceWithdrawn"),
  suspended: createAction<SuspendedDTO>("hubEvent/suspended"),
  banned: createAction<string>("hubEvent/banned"),
  anotherSessionStarted: createAction("hubEvent/anotherSessionStarted"),
};

export function receiverFor(dispatch: Dispatch): IGameClient {
  return {
    lobbyState: async (state) => {
      dispatch(hubEvents.lobbyState(state));
    },
    playerJoined: async (player) => {
      dispatch(hubEvents.playerJoined(player));
    },
    playerKicked: async (player) => {
      dispatch(hubEvents.playerKicked(player));
    },
    kicked: async () => {
      dispatch(hubEvents.kicked());
    },
    playerDisconnected: async (player) => {
      dispatch(hubEvents.playerDisconnected(player));
    },
    hostChanged: async (host) => {
      dispatch(hubEvents.hostChanged(host));
    },
    raceStarting: async (countdown) => {
      dispatch(hubEvents.raceStarting(countdown));
    },
    raceStarted: async () => {
      dispatch(hubEvents.raceStarted());
    },
    raceState: async (participants) => {
      dispatch(hubEvents.raceState(participants));
    },
    playerFinished: async (result) => {
      dispatch(hubEvents.playerFinished(result));
    },
    raceEnded: async (results) => {
      dispatch(hubEvents.raceEnded(results));
    },
    raceWithdrawn: async () => {
      dispatch(hubEvents.raceWithdrawn());
    },
    suspended: async (suspension) => {
      dispatch(hubEvents.suspended(suspension));
    },
    banned: async (reason) => {
      dispatch(hubEvents.banned(reason));
    },
    anotherSessionStarted: async () => {
      dispatch(hubEvents.anotherSessionStarted());
    },
  };
}
