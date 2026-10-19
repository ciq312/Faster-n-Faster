import { useTyping } from "./useTyping";
import { describe, it, expect, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useConnection } from "../../connection/ConnectionProvider";

vi.mock("../../connection/ConnectionProvider", () => ({
  useConnection: vi.fn(),
}));

describe("useTyping", () => {
  it("should advance the correct index for a correct character", () => {
    vi.mocked(useConnection).mockReturnValue({
      isConnected: true,
    });

    const onProgress = vi.fn();

    const { result } = renderHook(() =>
      useTyping({
        passage: "hi",
        disabled: false,
        onProgress,
        selfTyped: "",
        selfCorrectIndex: -1,
        selfMistakes: 0,
      }),
    );

    act(() => {
      result.current.handleTyping({
        target: { value: "h" },
      });
    });

    expect(result.current.typed).toBe("h");
    expect(onProgress).toHaveBeenLastCalledWith({
      index: 0,
      mistakes: 0,
      typed: "h",
      final: false,
    });
  });
});
