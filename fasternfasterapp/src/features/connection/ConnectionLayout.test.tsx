import { act, cleanup, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { fakeGameHub, type FakeGameHub } from "../../test/fakeGameHub";
import { renderPage } from "../../test/renderPage";
import ConnectionLayout from "./ConnectionLayout";

function renderLayout() {
  const hub = fakeGameHub();
  renderPage(<ConnectionLayout />, {
    route: "/lobbies",
    probes: ["/"],
    hub,
  });
  return hub;
}

describe("ConnectionLayout", () => {
  it("connects to the hub on mount", () => {
    const hub = renderLayout();

    expect(hub.start).toHaveBeenCalledTimes(1);
  });

  it("disconnects from the hub on unmount", () => {
    const hub = renderLayout();

    cleanup();

    expect(hub.stop).toHaveBeenCalledTimes(1);
  });

  it.each([
    { event: "banned", emit: (hub: FakeGameHub) => hub.client().banned("x") },
    {
      event: "suspended",
      emit: (hub: FakeGameHub) =>
        hub.client().suspended({ expiresAt: "2030-01-01" }),
    },
    {
      event: "anotherSessionStarted",
      emit: (hub: FakeGameHub) => hub.client().anotherSessionStarted(),
    },
  ])("returns home when $event arrives", async ({ emit }) => {
    const hub = renderLayout();

    await act(() => emit(hub));

    expect(await screen.findByText("/")).toBeTruthy();
  });
});
