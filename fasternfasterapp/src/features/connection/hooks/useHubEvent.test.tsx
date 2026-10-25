import { addListener } from "@reduxjs/toolkit";
import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { setupStore } from "../../../app/store";
import { fakeGameHub } from "../../../test/fakeGameHub";
import { wrapperFor } from "../../../test/store";
import { hubEvents } from "../hubEvents";
import { useHubEvent } from "./useHubEvent";

function setup() {
  const store = setupStore({ hub: fakeGameHub() });
  return { store, wrapper: wrapperFor(store) };
}

describe("useHubEvent", () => {
  it("calls the handler with each event payload", () => {
    const { store, wrapper } = setup();
    const handler = vi.fn();
    renderHook(() => useHubEvent(hubEvents.banned, handler), { wrapper });

    act(() => {
      store.dispatch(hubEvents.banned("first"));
      store.dispatch(hubEvents.banned("second"));
    });

    expect(handler.mock.calls).toEqual([["first"], ["second"]]);
  });

  it("calls the latest handler without resubscribing", () => {
    const { store, wrapper } = setup();
    const subscriptions = vi.spyOn(store, "dispatch");
    const first = vi.fn();
    const second = vi.fn();
    const { rerender } = renderHook(
      ({ handler }) => useHubEvent(hubEvents.banned, handler),
      { wrapper, initialProps: { handler: first } },
    );

    rerender({ handler: second });
    act(() => {
      store.dispatch(hubEvents.banned("reason"));
    });

    expect(first).not.toHaveBeenCalled();
    expect(second).toHaveBeenCalledWith("reason");
    expect(
      subscriptions.mock.calls.filter(([action]) => addListener.match(action)),
    ).toHaveLength(1);
  });

  it("stops calling the handler after unmount", () => {
    const { store, wrapper } = setup();
    const handler = vi.fn();
    const { unmount } = renderHook(
      () => useHubEvent(hubEvents.banned, handler),
      { wrapper },
    );

    unmount();
    store.dispatch(hubEvents.banned("reason"));

    expect(handler).not.toHaveBeenCalled();
  });
});
