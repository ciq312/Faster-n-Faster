import { http, HttpResponse } from "msw";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { API_BASE } from "../config";
import { authApi } from "../features/auth/authApi";
import { hubEvents } from "../features/connection/hubEvents";
import { connectHub } from "../features/connection/hubThunks";
import { progressTyped } from "../features/game/raceSlice";
import { selectCurrentBanner } from "../shared/banner/bannerSlice";
import { fakeGameHub, type FakeGameHub } from "../test/fakeGameHub";
import { server } from "../test/server";
import { setupStore } from "./store";

function setup() {
  const hub = fakeGameHub();
  const store = setupStore({ hub });
  return { hub, store };
}

describe("listener middleware", () => {
  it("shows a banner for a hub notification", async () => {
    const { hub, store } = setup();
    await store.dispatch(connectHub());

    await hub.client().playerKicked({ userId: "p2", nick: "Other" });

    await vi.waitFor(() =>
      expect(selectCurrentBanner(store.getState())?.message).toBe(
        "player Other was kicked from lobby",
      ),
    );
  });

  it.each([
    {
      event: "banned",
      emit: (hub: FakeGameHub) => hub.client().banned("reason"),
    },
    {
      event: "suspended",
      emit: (hub: FakeGameHub) =>
        hub.client().suspended({ expiresAt: "2030-01-01" }),
    },
    {
      event: "anotherSessionStarted",
      emit: (hub: FakeGameHub) => hub.client().anotherSessionStarted(),
    },
  ])("stops the hub when $event arrives", async ({ emit }) => {
    const { hub, store } = setup();
    await store.dispatch(connectHub());

    await emit(hub);

    await vi.waitFor(() => expect(hub.stop).toHaveBeenCalledTimes(1));
    expect(store.getState().connection.endReason).not.toBeNull();
  });

  it("disconnects the hub after a successful logout", async () => {
    const { hub, store } = setup();
    server.use(
      http.post(`${API_BASE}/api/auth/logout`, () => HttpResponse.json({})),
    );

    await store.dispatch(authApi.endpoints.logout.initiate());

    await vi.waitFor(() => expect(hub.stop).toHaveBeenCalledTimes(1));
  });

  it("keeps the hub connected when logout fails", async () => {
    const { hub, store } = setup();
    server.use(
      http.post(`${API_BASE}/api/auth/logout`, () =>
        HttpResponse.json({}, { status: 500 }),
      ),
    );

    await store.dispatch(authApi.endpoints.logout.initiate());

    expect(hub.stop).not.toHaveBeenCalled();
  });

  describe("progress throttle", () => {
    beforeEach(() => {
      vi.useFakeTimers();
    });
    afterEach(() => {
      vi.useRealTimers();
    });

    async function startRacing(store: ReturnType<typeof setup>["store"]) {
      await store.dispatch(connectHub());
      store.dispatch(hubEvents.raceStarted());
    }

    function progress(typed: string, final = false) {
      return progressTyped({
        index: typed.length - 1,
        mistakes: 0,
        typed,
        final,
      });
    }

    it("sends the first progress at once", async () => {
      const { hub, store } = setup();
      await startRacing(store);

      store.dispatch(progress("a"));

      expect(hub.server.updateRaceState).toHaveBeenCalledWith(0, 0, "a");
    });

    it("coalesces progress within 100 ms into one trailing send with the latest text", async () => {
      const { hub, store } = setup();
      await startRacing(store);

      store.dispatch(progress("a"));
      store.dispatch(progress("ab"));
      store.dispatch(progress("abc"));
      expect(hub.server.updateRaceState).toHaveBeenCalledTimes(1);

      await vi.advanceTimersByTimeAsync(100);

      expect(hub.server.updateRaceState.mock.calls).toEqual([
        [0, 0, "a"],
        [2, 0, "abc"],
      ]);
    });

    it("sends at once again after 100 ms have passed", async () => {
      const { hub, store } = setup();
      await startRacing(store);
      store.dispatch(progress("a"));

      await vi.advanceTimersByTimeAsync(100);
      store.dispatch(progress("ab"));

      expect(hub.server.updateRaceState).toHaveBeenCalledTimes(2);
    });

    it("sends final progress at once and drops the pending one", async () => {
      const { hub, store } = setup();
      await startRacing(store);

      store.dispatch(progress("a"));
      store.dispatch(progress("ab"));
      store.dispatch(progress("abc", true));

      expect(hub.server.updateRaceState).toHaveBeenCalledTimes(2);
      expect(hub.server.updateRaceState).toHaveBeenLastCalledWith(2, 0, "abc");

      await vi.advanceTimersByTimeAsync(100);

      expect(hub.server.updateRaceState).toHaveBeenCalledTimes(2);
    });

    it("drops pending progress after the player is withdrawn", async () => {
      const { hub, store } = setup();
      await startRacing(store);
      store.dispatch(progress("a"));
      store.dispatch(progress("ab"));

      store.dispatch(hubEvents.raceWithdrawn());
      await vi.advanceTimersByTimeAsync(100);

      expect(hub.server.updateRaceState).toHaveBeenCalledTimes(1);
    });

    it("drops pending progress when the race ends before it is sent", async () => {
      const { hub, store } = setup();
      await startRacing(store);
      store.dispatch(progress("a"));
      store.dispatch(progress("ab"));

      store.dispatch(hubEvents.raceEnded({ results: [] }));
      await vi.advanceTimersByTimeAsync(100);

      expect(hub.server.updateRaceState).toHaveBeenCalledTimes(1);
    });

    it("sends no progress outside a race", async () => {
      const { hub, store } = setup();
      await store.dispatch(connectHub());

      store.dispatch(progress("a"));
      await vi.advanceTimersByTimeAsync(100);

      expect(hub.server.updateRaceState).not.toHaveBeenCalled();
    });
  });
});
