import { describe, expect, it } from "vitest";
import { setupStore } from "../../app/store";
import { fakeGameHub } from "../../test/fakeGameHub";
import { selectIsConnected, selectSessionEndReason } from "./connectionSlice";
import { hubEvents } from "./hubEvents";
import { connectHub, disconnectHub } from "./hubThunks";

describe("connection slice", () => {
  it("starts disconnected", () => {
    const store = setupStore({ hub: fakeGameHub() });

    expect(selectIsConnected(store.getState())).toBe(false);
    expect(selectSessionEndReason(store.getState())).toBeNull();
  });

  it("follows the hub's reconnecting and reconnected status", async () => {
    const hub = fakeGameHub();
    const store = setupStore({ hub });
    await store.dispatch(connectHub());

    hub.setStatus("reconnecting");
    expect(selectIsConnected(store.getState())).toBe(false);

    hub.setStatus("connected");
    expect(selectIsConnected(store.getState())).toBe(true);
  });

  it.each([
    { reason: "banned", dispatch: () => hubEvents.banned("cheating") },
    {
      reason: "suspended",
      dispatch: () => hubEvents.suspended({ expiresAt: "2030-01-01" }),
    },
    {
      reason: "anotherSession",
      dispatch: () => hubEvents.anotherSessionStarted(),
    },
  ] as const)(
    "records $reason as the reason the session ended",
    async ({ reason, dispatch }) => {
      const hub = fakeGameHub();
      const store = setupStore({ hub });
      await store.dispatch(connectHub());

      store.dispatch(dispatch());

      expect(selectSessionEndReason(store.getState())).toBe(reason);
      expect(selectIsConnected(store.getState())).toBe(false);
    },
  );

  it("clears the end reason when the hub disconnects", async () => {
    const store = setupStore({ hub: fakeGameHub() });
    store.dispatch(hubEvents.banned("cheating"));

    await store.dispatch(disconnectHub());

    expect(selectSessionEndReason(store.getState())).toBeNull();
  });
});
