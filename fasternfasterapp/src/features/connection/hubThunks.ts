import { createAction, type AsyncThunk } from "@reduxjs/toolkit";
import type { KeystrokeEventDTO } from "../../../generated/signalr/FasterNFaster.Api.Presentation.Web.Realtime";
import type { RaceProgressDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Races.GetRaceProgress";
import {
  createAppAsyncThunk,
  type AppThunkConfig,
} from "../../app/createAppAsyncThunk";
import { extractHubError } from "../../shared/utils/extractHubError";
import type { GameHubServer, HubStatus } from "./gameHub";
import { receiverFor } from "./hubEvents";

export type JoinLobbyArgs = { lobbyId: string; inviteCode?: string | null };

export const hubStatusChanged = createAction<HubStatus>(
  "connection/statusChanged",
);

export const connectHub = createAppAsyncThunk(
  "connection/connect",
  async (_: void, { dispatch, extra }) => {
    await extra.hub.start(receiverFor(dispatch), (status) =>
      dispatch(hubStatusChanged(status)),
    );
  },
);

export const disconnectHub = createAppAsyncThunk(
  "connection/disconnect",
  async (_: void, { extra }) => {
    await extra.hub.stop();
  },
);

// A connection keeps the nick from the token it started with, so a rename needs a new one.
export const reconnectHub = createAppAsyncThunk(
  "connection/reconnect",
  async (_: void, { dispatch }) => {
    await dispatch(disconnectHub());
    await dispatch(connectHub());
  },
);

function hubCall<Arg, Result = void>(
  name: string,
  call: (server: GameHubServer, arg: Arg) => Promise<Result>,
): AsyncThunk<Result, Arg, AppThunkConfig> {
  return createAppAsyncThunk<Result, Arg>(
    `hubCall/${name}`,
    async (arg, { extra, rejectWithValue }) => {
      try {
        return await call(extra.hub.server, arg);
      } catch (e) {
        console.error(e);
        return rejectWithValue(extractHubError(e));
      }
    },
  );
}

export const connectToLobby = hubCall<JoinLobbyArgs>(
  "connectToLobby",
  (server, { lobbyId, inviteCode }) =>
    server.connectToLobby(lobbyId, inviteCode ?? null),
);
export const leaveLobby = hubCall<void>("leaveLobby", (server) =>
  server.leaveLobby(),
);
export const refreshLobby = hubCall<void>("refreshLobby", (server) =>
  server.refreshLobby(),
);
export const changeColor = hubCall<string>("changeColor", (server, color) =>
  server.changeColor(color),
);
export const kickPlayer = hubCall<string>("kickPlayer", (server, targetId) =>
  server.kickPlayer(targetId),
);
export const transferHost = hubCall<string>(
  "transferHost",
  (server, targetId) => server.transferHost(targetId),
);
export const startRace = hubCall<void>("startRace", (server) =>
  server.startRace(),
);
export const refreshPassage = hubCall<void>("refreshPassage", (server) =>
  server.refreshPassage(),
);
export const updateRaceState = hubCall<KeystrokeEventDTO[]>(
  "updateRaceState",
  (server, keystrokes) => server.updateRaceState(keystrokes),
);
export const getRaceProgress = hubCall<void, RaceProgressDTO | null>(
  "getRaceProgress",
  (server) => server.getRaceProgress(),
);

export const bannerHubCalls = [
  connectToLobby,
  leaveLobby,
  refreshLobby,
  changeColor,
  kickPlayer,
  transferHost,
  startRace,
  refreshPassage,
  getRaceProgress,
] as const;
