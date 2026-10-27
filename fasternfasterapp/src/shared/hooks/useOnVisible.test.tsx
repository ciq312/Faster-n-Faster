import { act, render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { revealObserved } from "../../test/fakeIntersectionObserver";
import { useOnVisible } from "./useOnVisible";

function Probe({ onVisible }: { onVisible: () => void }) {
  const ref = useOnVisible(onVisible);
  return <div ref={ref} />;
}

describe("useOnVisible", () => {
  it("calls back when the element comes into view", () => {
    const onVisible = vi.fn();
    render(<Probe onVisible={onVisible} />);

    act(() => revealObserved());

    expect(onVisible).toHaveBeenCalledTimes(1);
  });

  it("does not call back before the element is visible", () => {
    const onVisible = vi.fn();
    render(<Probe onVisible={onVisible} />);

    expect(onVisible).not.toHaveBeenCalled();
  });

  it("stops observing after unmount", () => {
    const onVisible = vi.fn();
    const { unmount } = render(<Probe onVisible={onVisible} />);

    unmount();
    act(() => revealObserved());

    expect(onVisible).not.toHaveBeenCalled();
  });

  it("calls the latest callback after a rerender", () => {
    const first = vi.fn();
    const second = vi.fn();
    const { rerender } = render(<Probe onVisible={first} />);

    rerender(<Probe onVisible={second} />);
    act(() => revealObserved());

    expect(first).not.toHaveBeenCalled();
    expect(second).toHaveBeenCalledTimes(1);
  });
});
