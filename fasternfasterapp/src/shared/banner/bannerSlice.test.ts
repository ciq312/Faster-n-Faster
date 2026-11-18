import { describe, expect, it } from "vitest";
import {
  bannerDismissed,
  bannerSlice,
  showError,
  showMessage,
} from "./bannerSlice";

const empty = bannerSlice.getInitialState();

describe("bannerSlice", () => {
  it("shows a banner at once when none is showing", () => {
    const action = showError("boom");

    const state = bannerSlice.reducer(empty, action);

    expect(state.current).toEqual({
      id: action.payload.id,
      variant: "error",
      message: "boom",
    });
    expect(state.next).toBeNull();
  });

  it("queues a banner while another is showing", () => {
    const first = showError("first");
    const second = showMessage("second");

    const state = [first, second].reduce(bannerSlice.reducer, empty);

    expect(state.current?.message).toBe("first");
    expect(state.next).toEqual({
      id: second.payload.id,
      variant: "message",
      message: "second",
    });
  });

  it("replaces the queued banner with a newer one", () => {
    const state = [
      showError("first"),
      showError("second"),
      showError("third"),
    ].reduce(bannerSlice.reducer, empty);

    expect(state.current?.message).toBe("first");
    expect(state.next?.message).toBe("third");
  });

  it("promotes the queued banner when the current one is dismissed", () => {
    const first = showError("first");
    const shown = [first, showError("second")].reduce(
      bannerSlice.reducer,
      empty,
    );

    const state = bannerSlice.reducer(shown, bannerDismissed(first.payload.id));

    expect(state.current?.message).toBe("second");
    expect(state.next).toBeNull();
  });

  it("ignores a dismissal for a banner that is no longer current", () => {
    const first = showError("first");
    const dismissFirst = bannerDismissed(first.payload.id);
    const promoted = [first, showError("second"), dismissFirst].reduce(
      bannerSlice.reducer,
      empty,
    );

    const state = bannerSlice.reducer(promoted, dismissFirst);

    expect(state).toEqual(promoted);
  });

  it("gives each banner its own id", () => {
    expect(showError("same").payload.id).not.toBe(showError("same").payload.id);
  });
});
