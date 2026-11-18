import { createAction, createSlice } from "@reduxjs/toolkit";
import type { RaceResultDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceFinished";
import type { RaceParticipantDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceState";
import { hubEvents } from "../connection/hubEvents";
import { disconnectHub, leaveLobby } from "../connection/hubThunks";
import type { TypingProgress } from "./hooks/useTyping";

export type RacePhase = "idle" | "countdown" | "racing" | "finished";

type RaceSliceState =
  | { phase: "idle" }
  | { phase: "countdown"; seconds: number | null }
  | { phase: "racing"; participants: RaceParticipantDTO[]; withdrawn: boolean }
  | { phase: "finished"; results: RaceResultDTO[] };

const NO_PARTICIPANTS: RaceParticipantDTO[] = [];

const initialState = { phase: "idle" } as RaceSliceState;

function racing(): RaceSliceState {
  return { phase: "racing", participants: [], withdrawn: false };
}

export const progressTyped = createAction<TypingProgress>("race/progressTyped");

export const raceSlice = createSlice({
  name: "race",
  initialState,
  reducers: {
    resultsDismissed: (state) =>
      state.phase === "finished" ? initialState : state,
  },
  extraReducers: (builder) =>
    builder
      .addCase(hubEvents.lobbyState, (state, { payload }) => {
        if (payload.isRaceStarted) {
          return state.phase === "racing" ? state : racing();
        }
        if (payload.isRaceStarting) {
          return state.phase === "countdown"
            ? state
            : { phase: "countdown", seconds: null };
        }
        return state.phase === "finished" ? state : initialState;
      })
      .addCase(hubEvents.raceStarting, (_, { payload }) => ({
        phase: "countdown",
        seconds: payload.countdownSeconds,
      }))
      .addCase(hubEvents.raceStarted, racing)
      .addCase(hubEvents.raceState, (state, { payload }) => {
        if (state.phase === "racing") state.participants = payload;
      })
      .addCase(hubEvents.raceWithdrawn, (state) => {
        if (state.phase === "racing") state.withdrawn = true;
      })
      .addCase(hubEvents.raceEnded, (_, { payload }) => ({
        phase: "finished",
        results: payload.results,
      }))
      .addCase(hubEvents.kicked, () => initialState)
      .addCase(leaveLobby.fulfilled, () => initialState)
      .addCase(disconnectHub.pending, () => initialState),
  selectors: {
    selectRacePhase: (state): RacePhase => state.phase,
    selectCountdownSeconds: (state) =>
      state.phase === "countdown" ? state.seconds : null,
    selectRaceParticipants: (state) =>
      state.phase === "racing" ? state.participants : NO_PARTICIPANTS,
    selectRaceResults: (state) =>
      state.phase === "finished" ? state.results : null,
    selectCanType: (state) => state.phase === "racing" && !state.withdrawn,
  },
});

export const { resultsDismissed } = raceSlice.actions;
export const {
  selectRacePhase,
  selectCountdownSeconds,
  selectRaceParticipants,
  selectRaceResults,
  selectCanType,
} = raceSlice.selectors;
