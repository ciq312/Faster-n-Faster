import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useCooldown } from "./useCooldown";

describe("useCooldown", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("starts at zero unless started active", () => {
    const { result } = renderHook(() => useCooldown(15));

    expect(result.current.cooldown).toBe(0);
  });

  it("starts at the full duration when started active", () => {
    const { result } = renderHook(() => useCooldown(15, { startActive: true }));

    expect(result.current.cooldown).toBe(15);
  });

  it("counts down to zero one second at a time", () => {
    const { result } = renderHook(() => useCooldown(2, { startActive: true }));

    act(() => vi.advanceTimersByTime(1000));
    expect(result.current.cooldown).toBe(1);

    act(() => vi.advanceTimersByTime(1000));
    expect(result.current.cooldown).toBe(0);

    act(() => vi.advanceTimersByTime(5000));
    expect(result.current.cooldown).toBe(0);
  });

  it("restarts from the full duration when started again", () => {
    const { result } = renderHook(() => useCooldown(3, { startActive: true }));
    act(() => vi.advanceTimersByTime(2000));

    act(() => result.current.start());

    expect(result.current.cooldown).toBe(3);
  });
});
