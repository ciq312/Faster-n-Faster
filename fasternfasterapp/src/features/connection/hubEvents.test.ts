import { describe, expect, it, vi } from "vitest";
import type { IGameClient } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.UseCases.Realtime.Ports";
import { lobbyState } from "../../test/fixtures";
import { hubEvents, receiverFor } from "./hubEvents";

type Event = keyof IGameClient;

const payloads: { [E in Event]: Parameters<IGameClient[E]> } = {
  lobbyState: [lobbyState()],
  playerJoined: [{ playerId: "p2", displayName: "Other" }],
  playerKicked: [{ userId: "p2", nick: "Other" }],
  kicked: [],
  playerDisconnected: [
    { disconnectedUserId: "p2", disconnectedUserNick: "Other" },
  ],
  hostChanged: [{ userId: "p2", newHostNick: "Other" }],
  raceStarting: [{ countdownSeconds: 3 }],
  raceStarted: [],
  raceState: [
    [
      {
        playerId: "p1",
        index: 1,
        typed: "hi",
        wpm: 50,
        color: "red",
        nick: "Player",
        mistakes: 0,
      },
    ],
  ],
  playerFinished: [
    {
      nick: "Player",
      playerId: "p1",
      finishPosition: 1,
      wpm: 50,
      accuracy: 99,
    },
  ],
  raceEnded: [{ results: [] }],
  raceWithdrawn: [],
  suspended: [{ expiresAt: "2030-01-01T00:00:00Z" }],
  banned: ["cheating"],
  anotherSessionStarted: [],
};

const events = Object.keys(payloads) as Event[];

describe("receiverFor", () => {
  it("covers every client event", () => {
    expect(events).toHaveLength(15);
    expect(Object.keys(hubEvents).sort()).toEqual([...events].sort());
  });

  it.each(events.map((event) => ({ event })))(
    "dispatches $event as its action",
    async ({ event }) => {
      const dispatch = vi.fn();
      const args = payloads[event] as unknown[];
      const receive = receiverFor(dispatch)[event] as (
        ...a: unknown[]
      ) => Promise<void>;
      const create = hubEvents[event] as (...a: unknown[]) => unknown;

      await receive(...args);

      expect(dispatch).toHaveBeenCalledWith(create(...args));
    },
  );
});
