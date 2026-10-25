import { createSlice } from "@reduxjs/toolkit";
import type { HubStatus } from "./gameHub";
import { hubEvents } from "./hubEvents";
import { connectHub, disconnectHub, hubStatusChanged } from "./hubThunks";

export type ConnectionStatus = HubStatus | "connecting";
export type SessionEndReason = "anotherSession" | "banned" | "suspended";

type ConnectionState = {
  status: ConnectionStatus;
  endReason: SessionEndReason | null;
};

const initialState: ConnectionState = {
  status: "disconnected",
  endReason: null,
};

function endSession(reason: SessionEndReason) {
  return (state: ConnectionState) => {
    state.status = "disconnected";
    state.endReason = reason;
  };
}

export const connectionSlice = createSlice({
  name: "connection",
  initialState,
  reducers: {},
  extraReducers: (builder) =>
    builder
      .addCase(connectHub.pending, (state) => {
        state.status = "connecting";
      })
      .addCase(connectHub.rejected, (state) => {
        state.status = "disconnected";
      })
      .addCase(hubStatusChanged, (state, { payload }) => {
        state.status = payload;
      })
      .addCase(hubEvents.banned, endSession("banned"))
      .addCase(hubEvents.suspended, endSession("suspended"))
      .addCase(hubEvents.anotherSessionStarted, endSession("anotherSession"))
      .addCase(disconnectHub.pending, (state) => {
        state.status = "disconnected";
        state.endReason = null;
      }),
  selectors: {
    selectIsConnected: (state) => state.status === "connected",
    selectSessionEndReason: (state) => state.endReason,
  },
});

export const { selectIsConnected, selectSessionEndReason } =
  connectionSlice.selectors;
