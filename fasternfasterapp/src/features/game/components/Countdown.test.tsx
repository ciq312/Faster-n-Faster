import { act, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import Countdown from "./Countdown";

describe("Countdown", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("counts down from the given seconds", () => {
    render(<Countdown seconds={3} />);
    expect(screen.getByText("3")).toBeTruthy();

    act(() => vi.advanceTimersByTime(1000));
    expect(screen.getByText("2")).toBeTruthy();

    act(() => vi.advanceTimersByTime(1000));
    expect(screen.getByText("1")).toBeTruthy();
  });

  it("shows GO once the seconds run out and stays on it", () => {
    render(<Countdown seconds={1} />);

    act(() => vi.advanceTimersByTime(1000));
    expect(screen.getByText("GO")).toBeTruthy();

    act(() => vi.advanceTimersByTime(5000));
    expect(screen.getByText("GO")).toBeTruthy();
  });
});
