import { describe, expect, it } from "vitest";
import { setupStore } from "../../app/store";
import { fakeGameHub } from "../../test/fakeGameHub";
import { lobbyState, raceParticipant, raceResult } from "../../test/fixtures";
import { hubEvents } from "../connection/hubEvents";
import { disconnectHub, leaveLobby } from "../connection/hubThunks";
import {
  resultsDismissed,
  selectCanType,
  selectCountdownSeconds,
  selectRaceParticipants,
  selectRacePhase,
  selectRaceResults,
} from "./raceSlice";

const participant = raceParticipant();
const results = [raceResult()];

function setup() {
  return setupStore({ hub: fakeGameHub() });
}

describe("race slice", () => {
  it("starts idle with typing locked", () => {
    const store = setup();

    expect(selectRacePhase(store.getState())).toBe("idle");
    expect(selectCanType(store.getState())).toBe(false);
    expect(selectCountdownSeconds(store.getState())).toBeNull();
    expect(selectRaceResults(store.getState())).toBeNull();
    expect(selectRaceParticipants(store.getState())).toEqual([]);
  });

  it("enters the countdown with its seconds when the race is starting", () => {
    const store = setup();

    store.dispatch(hubEvents.raceStarting({ countdownSeconds: 3 }));

    expect(selectRacePhase(store.getState())).toBe("countdown");
    expect(selectCountdownSeconds(store.getState())).toBe(3);
  });

  it.each([
    {
      event: "RaceStarting",
      action: () => hubEvents.raceStarting({ countdownSeconds: 3 }),
    },
    {
      event: "LobbyState",
      action: () => hubEvents.lobbyState(lobbyState({ isRaceStarting: true })),
    },
  ])("never enables typing during the countdown from $event", ({ action }) => {
    const store = setup();

    store.dispatch(action());

    expect(selectCanType(store.getState())).toBe(false);
  });

  it("enters the countdown without seconds when LobbyState says the race is starting", () => {
    const store = setup();

    store.dispatch(hubEvents.lobbyState(lobbyState({ isRaceStarting: true })));

    expect(selectRacePhase(store.getState())).toBe("countdown");
    expect(selectCountdownSeconds(store.getState())).toBeNull();
  });

  it("keeps the countdown seconds when LobbyState repeats the countdown", () => {
    const store = setup();
    store.dispatch(hubEvents.raceStarting({ countdownSeconds: 3 }));

    store.dispatch(hubEvents.lobbyState(lobbyState({ isRaceStarting: true })));

    expect(selectCountdownSeconds(store.getState())).toBe(3);
  });

  it("enables typing once the race starts", () => {
    const store = setup();
    store.dispatch(hubEvents.raceStarting({ countdownSeconds: 3 }));

    store.dispatch(hubEvents.raceStarted());

    expect(selectRacePhase(store.getState())).toBe("racing");
    expect(selectCanType(store.getState())).toBe(true);
    expect(selectCountdownSeconds(store.getState())).toBeNull();
  });

  it("resumes racing when LobbyState arrives with a started race after a reload", () => {
    const store = setup();

    store.dispatch(hubEvents.lobbyState(lobbyState({ isRaceStarted: true })));

    expect(selectRacePhase(store.getState())).toBe("racing");
    expect(selectCanType(store.getState())).toBe(true);
  });

  it("keeps participants and the withdrawal when LobbyState repeats a race in progress", () => {
    const store = setup();
    store.dispatch(hubEvents.raceStarted());
    store.dispatch(hubEvents.raceState([participant]));
    store.dispatch(hubEvents.raceWithdrawn());

    store.dispatch(hubEvents.lobbyState(lobbyState({ isRaceStarted: true })));

    expect(selectRaceParticipants(store.getState())).toEqual([participant]);
    expect(selectCanType(store.getState())).toBe(false);
  });

  it("ignores RaceState outside a race", () => {
    const store = setup();

    store.dispatch(hubEvents.raceState([participant]));

    expect(selectRacePhase(store.getState())).toBe("idle");
    expect(selectRaceParticipants(store.getState())).toEqual([]);
  });

  it("stores RaceState while racing", () => {
    const store = setup();
    store.dispatch(hubEvents.raceStarted());

    store.dispatch(hubEvents.raceState([participant]));

    expect(selectRaceParticipants(store.getState())).toEqual([participant]);
  });

  it("blocks typing after RaceWithdrawn until the next race starts", () => {
    const store = setup();
    store.dispatch(hubEvents.raceStarted());

    store.dispatch(hubEvents.raceWithdrawn());
    expect(selectCanType(store.getState())).toBe(false);

    store.dispatch(hubEvents.lobbyState(lobbyState({ isRaceStarted: true })));
    expect(selectCanType(store.getState())).toBe(false);

    store.dispatch(hubEvents.raceStarting({ countdownSeconds: 3 }));
    expect(selectCanType(store.getState())).toBe(false);

    store.dispatch(hubEvents.raceStarted());
    expect(selectCanType(store.getState())).toBe(true);
  });

  it("shows the results when the race ends", () => {
    const store = setup();
    store.dispatch(hubEvents.raceStarted());

    store.dispatch(hubEvents.raceEnded({ results }));

    expect(selectRacePhase(store.getState())).toBe("finished");
    expect(selectRaceResults(store.getState())).toEqual(results);
    expect(selectCanType(store.getState())).toBe(false);
    expect(selectRaceParticipants(store.getState())).toEqual([]);
  });

  it("returns to idle when the results are dismissed", () => {
    const store = setup();
    store.dispatch(hubEvents.raceEnded({ results }));

    store.dispatch(resultsDismissed());

    expect(selectRacePhase(store.getState())).toBe("idle");
    expect(selectRaceResults(store.getState())).toBeNull();
  });

  it("keeps the results when LobbyState reports no race", () => {
    const store = setup();
    store.dispatch(hubEvents.raceEnded({ results }));

    store.dispatch(hubEvents.lobbyState(lobbyState()));

    expect(selectRaceResults(store.getState())).toEqual(results);
  });

  it("returns to idle when LobbyState reports no race during a race", () => {
    const store = setup();
    store.dispatch(hubEvents.raceStarted());

    store.dispatch(hubEvents.lobbyState(lobbyState()));

    expect(selectRacePhase(store.getState())).toBe("idle");
  });

  it("clears old results when the next race is starting", () => {
    const store = setup();
    store.dispatch(hubEvents.raceEnded({ results }));

    store.dispatch(hubEvents.raceStarting({ countdownSeconds: 3 }));

    expect(selectRaceResults(store.getState())).toBeNull();
    expect(selectRacePhase(store.getState())).toBe("countdown");
  });

  it.each([
    { event: "kicked", action: () => hubEvents.kicked() },
    { event: "leaving", action: () => leaveLobby.fulfilled(undefined, "id") },
    { event: "disconnect", action: () => disconnectHub.pending("id") },
  ])("resets on $event", ({ action }) => {
    const store = setup();
    store.dispatch(hubEvents.raceStarted());

    store.dispatch(action());

    expect(selectRacePhase(store.getState())).toBe("idle");
  });
});
