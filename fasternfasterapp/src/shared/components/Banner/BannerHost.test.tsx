import { act, render, screen } from "@testing-library/react";
import { StrictMode } from "react";
import { Provider } from "react-redux";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { setupStore } from "../../../app/store";
import { showError, showMessage } from "../../banner/bannerSlice";
import BannerHost from "./BannerHost";

function renderHost({ strict = false, store = setupStore() } = {}) {
  const host = (
    <Provider store={store}>
      <BannerHost />
    </Provider>
  );
  const { unmount } = render(strict ? <StrictMode>{host}</StrictMode> : host);
  return { store, unmount };
}

function advance(ms: number) {
  act(() => {
    vi.advanceTimersByTime(ms);
  });
}

describe("BannerHost", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("shows the current banner", () => {
    const { store } = renderHost();

    act(() => {
      store.dispatch(showError("boom"));
    });

    expect(screen.getByText("boom").className).toContain("error-banner");
  });

  it("fades the banner before removing it", () => {
    const { store } = renderHost();
    act(() => {
      store.dispatch(showError("boom"));
    });

    advance(3599);
    expect(screen.getByText("boom").className).not.toContain("--fading");

    advance(1);
    expect(screen.getByText("boom").className).toContain(
      "error-banner--fading",
    );
  });

  it("removes the banner after four seconds", () => {
    const { store } = renderHost();
    act(() => {
      store.dispatch(showError("boom"));
    });

    advance(4000);

    expect(screen.queryByText("boom")).toBeNull();
  });

  it("shows the queued banner after the current one is removed", () => {
    const { store } = renderHost();
    act(() => {
      store.dispatch(showError("first"));
      store.dispatch(showMessage("second"));
    });
    expect(screen.queryByText("second")).toBeNull();

    advance(4000);

    expect(screen.getByText("second").className).toContain("message-banner");
    expect(screen.getByText("second").className).not.toContain("--fading");
    expect(screen.queryByText("first")).toBeNull();

    advance(4000);

    expect(screen.queryByText("second")).toBeNull();
  });

  it("keeps showing banners after a StrictMode effect replay", () => {
    const { store } = renderHost({ strict: true });
    act(() => {
      store.dispatch(showError("first"));
    });
    advance(4000);
    expect(screen.queryByText("first")).toBeNull();

    act(() => {
      store.dispatch(showError("second"));
    });

    expect(screen.getByText("second")).toBeTruthy();
    advance(4000);
    expect(screen.queryByText("second")).toBeNull();
  });

  it("keeps showing banners after the host remounts mid-banner", () => {
    const { store, unmount } = renderHost();
    act(() => {
      store.dispatch(showError("first"));
    });
    advance(2000);

    unmount();
    renderHost({ store });

    expect(screen.getByText("first")).toBeTruthy();
    advance(4000);
    expect(screen.queryByText("first")).toBeNull();

    act(() => {
      store.dispatch(showError("second"));
    });

    expect(screen.getByText("second")).toBeTruthy();
  });
});
