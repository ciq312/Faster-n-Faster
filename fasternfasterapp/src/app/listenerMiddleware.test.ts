import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { API_BASE } from "../config";
import { authApi } from "../features/auth/authApi";
import { connectHub } from "../features/connection/hubThunks";
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
});
