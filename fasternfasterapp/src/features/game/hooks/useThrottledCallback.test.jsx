import { Activity } from "react";
import { useThrottledCallback } from "./useThrottledCallback";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, renderHook, act } from "@testing-library/react";

describe("useThrottledCallback", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  function renderThrottled(fn) {
    return renderHook(({ fn }) => useThrottledCallback(fn, 100), {
      initialProps: { fn },
    });
  }

  it("calls immediately on the first call", () => {
    const fn = vi.fn();
    const { result } = renderThrottled(fn);

    act(() => result.current.throttled(1));

    expect(fn).toHaveBeenCalledTimes(1);
    expect(fn).toHaveBeenCalledWith(1);
  });

  it("coalesces calls inside the interval into one trailing call with the last args", () => {
    const fn = vi.fn();
    const { result } = renderThrottled(fn);

    act(() => result.current.throttled(1));
    expect(fn.mock.calls).toEqual([[1]]);

    act(() => {
      result.current.throttled(2);
      result.current.throttled(3);
    });
    expect(fn.mock.calls).toEqual([[1]]);

    act(() => vi.advanceTimersByTime(100));
    expect(fn.mock.calls).toEqual([[1], [3]]);
  });

  it("calls immediately again once the interval has passed", () => {
    const fn = vi.fn();
    const { result } = renderThrottled(fn);

    act(() => result.current.throttled(1));
    act(() => vi.advanceTimersByTime(100));
    act(() => result.current.throttled(2));

    expect(fn.mock.calls).toEqual([[1], [2]]);
  });

  it("flush sends the pending call at once and cancels the timer", () => {
    const fn = vi.fn();
    const { result } = renderThrottled(fn);

    act(() => {
      result.current.throttled(1);
      result.current.throttled(2);
      result.current.flush();
    });
    expect(fn.mock.calls).toEqual([[1], [2]]);

    act(() => vi.advanceTimersByTime(100));
    expect(fn).toHaveBeenCalledTimes(2);
  });

  it("flush does nothing when nothing is pending", () => {
    const fn = vi.fn();
    const { result } = renderThrottled(fn);

    act(() => result.current.flush());

    expect(fn).not.toHaveBeenCalled();
  });

  it("drops the pending trailing call on unmount", () => {
    const fn = vi.fn();
    const { result, unmount } = renderThrottled(fn);

    act(() => {
      result.current.throttled(1);
      result.current.throttled(2);
    });
    unmount();
    act(() => vi.advanceTimersByTime(100));

    expect(fn.mock.calls).toEqual([[1]]);
  });

  it("uses the latest fn for the trailing call", () => {
    const fn1 = vi.fn();
    const fn2 = vi.fn();
    const { result, rerender } = renderThrottled(fn1);

    act(() => {
      result.current.throttled(1);
      result.current.throttled(2);
    });
    rerender({ fn: fn2 });
    act(() => vi.advanceTimersByTime(100));

    expect(fn1.mock.calls).toEqual([[1]]);
    expect(fn2.mock.calls).toEqual([[2]]);
  });

  it("still sends a trailing call after effects replay with a timer pending", () => {
    const fn = vi.fn();
    const hook = {};
    function Probe() {
      hook.current = useThrottledCallback(fn, 100);
      return null;
    }
    function Harness({ mode }) {
      return (
        <Activity mode={mode}>
          <Probe />
        </Activity>
      );
    }

    const { rerender } = render(<Harness mode="visible" />);
    act(() => {
      hook.current.throttled(1);
      hook.current.throttled(2);
    });
    rerender(<Harness mode="hidden" />);
    rerender(<Harness mode="visible" />);
    act(() => {
      hook.current.throttled(3);
      vi.advanceTimersByTime(100);
    });

    expect(fn).toHaveBeenLastCalledWith(3);
  });
});
