import { createAction, type AsyncThunk } from "@reduxjs/toolkit";
import {
  createAppAsyncThunk,
  type AppThunkConfig,
} from "../../app/createAppAsyncThunk";
import { extractHubError } from "../../shared/utils/extractHubError";
import type { GameHubServer, HubStatus } from "./gameHub";
import { receiverFor } from "./hubEvents";

export type JoinLobbyArgs = { lobbyId: string; inviteCode?: string | null };
export type RaceProgress = { index: number; mistakes: number; typed: string };

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

function hubCall<Arg>(
  name: string,
  call: (server: GameHubServer, arg: Arg) => Promise<unknown>,
): AsyncThunk<void, Arg, AppThunkConfig> {
  return createAppAsyncThunk<void, Arg>(
    `hubCall/${name}`,
    async (arg, { extra, rejectWithValue }) => {
      try {
        await call(extra.hub.server, arg);
      } catch (e) {
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
export const updateRaceState = hubCall<RaceProgress>(
  "updateRaceState",
  (server, { index, mistakes, typed }) =>
    server.updateRaceState(index, mistakes, typed),
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
] as const;
