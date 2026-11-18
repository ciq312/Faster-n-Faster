import { createSelector } from "@reduxjs/toolkit";
import { selectPlayers } from "../lobbies/lobbySelectors";
import { selectRaceParticipants } from "./raceSlice";

export type RaceCaret = {
  playerId: string;
  index: number;
  color: string;
  nick: string;
};

export const selectRaceCarets = createSelector(
  [selectRaceParticipants, selectPlayers],
  (participants, players): RaceCaret[] =>
    participants.flatMap(({ playerId, index }) => {
      const player = players.find((p) => p.id === playerId);
      return player
        ? [{ playerId, index, color: player.color, nick: player.nick }]
        : [];
    }),
);
