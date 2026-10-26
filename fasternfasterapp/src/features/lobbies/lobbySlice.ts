import { createSlice } from "@reduxjs/toolkit";
import type { LobbyStateDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Lobbies.LobbyState";
import { hubEvents } from "../connection/hubEvents";
import {
  connectToLobby,
  disconnectHub,
  leaveLobby,
} from "../connection/hubThunks";

type LobbySliceState = { lastState: LobbyStateDTO | null; kicked: boolean };

const initialState: LobbySliceState = { lastState: null, kicked: false };

export const lobbySlice = createSlice({
  name: "lobby",
  initialState,
  reducers: {},
  extraReducers: (builder) =>
    builder
      .addCase(hubEvents.lobbyState, (state, { payload }) => {
        state.lastState = payload;
      })
      .addCase(hubEvents.kicked, () => ({ lastState: null, kicked: true }))
      .addCase(leaveLobby.fulfilled, () => initialState)
      .addCase(disconnectHub.pending, () => initialState)
      .addCase(connectToLobby.pending, (state) => {
        state.kicked = false;
      }),
});
