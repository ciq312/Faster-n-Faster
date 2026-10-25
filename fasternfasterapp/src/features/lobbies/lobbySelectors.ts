import { createSelector } from "@reduxjs/toolkit";
import type { LobbyPlayerDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Lobbies.LobbyState";
import type { RootState } from "../../app/store";
import { selectIsSelf } from "../auth/authSelectors";

const NO_PLAYERS: LobbyPlayerDTO[] = [];

const selectLastState = (state: RootState) => state.lobby.lastState;

export const selectJoinedLobby = createSelector(
  [selectLastState, selectIsSelf],
  (lastState, isSelf) =>
    lastState?.players.some((p) => isSelf(p.id)) ? lastState : null,
);

export const selectLobbyId = (state: RootState) =>
  selectJoinedLobby(state)?.lobbyId ?? null;

export const selectPlayers = (state: RootState) =>
  selectJoinedLobby(state)?.players ?? NO_PLAYERS;

export const selectIsHost = createSelector(
  [selectPlayers, selectIsSelf],
  (players, isSelf) => players.find((p) => isSelf(p.id))?.isHost ?? false,
);

export const selectWasKicked = (state: RootState) => state.lobby.kicked;
