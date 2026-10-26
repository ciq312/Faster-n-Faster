import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { setupStore } from "../../../app/store";
import { fakeGameHub } from "../../../test/fakeGameHub";
import { wrapperFor } from "../../../test/store";
import { hubStatusChanged } from "../../connection/hubThunks";
import { useTyping, type TypingProgress } from "./useTyping";

type TypingOptions = Parameters<typeof useTyping>[0];

describe("useTyping", () => {
  let onProgress: Mock<(progress: TypingProgress) => void>;
  let store: ReturnType<typeof setupStore>;

  beforeEach(() => {
    store = setupStore({ hub: fakeGameHub() });
    act(() => {
      store.dispatch(hubStatusChanged("connected"));
    });
    onProgress = vi.fn();
  });

  function typingProps(overrides?: Partial<TypingOptions>): TypingOptions {
    return {
      passage: "hi",
      disabled: false,
      onProgress,
      selfTyped: "",
      selfCorrectIndex: -1,
      selfMistakes: 0,
      ...overrides,
    };
  }

  function renderTyping(overrides?: Partial<TypingOptions>) {
    return renderHook((p: TypingOptions) => useTyping(p), {
      initialProps: typingProps(overrides),
      wrapper: wrapperFor(store),
    });
  }

  function setConnected(
    rerender: (props: TypingOptions) => void,
    props: TypingOptions,
    isConnected: boolean,
  ) {
    act(() => {
      store.dispatch(
        hubStatusChanged(isConnected ? "connected" : "disconnected"),
      );
    });
    rerender(props);
  }

  type Typing = ReturnType<typeof renderTyping>["result"];

  function typeText(result: Typing, text: string) {
    for (const ch of text) {
      act(() => {
        result.current.handleTyping({
          target: { value: result.current.typed + ch },
        });
      });
    }
  }

  function backspace(result: Typing) {
    act(() => {
      result.current.handleTyping({
        target: { value: result.current.typed.slice(0, -1) },
      });
    });
  }

  it("advances the correct index for a correct character", () => {
    const { result } = renderTyping();

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

  it("counts a wrong character as a mistake without advancing the index", () => {
    const { result } = renderTyping({ passage: "ab" });

    typeText(result, "x");

    expect(result.current.typed).toBe("x");
    expect(onProgress).toHaveBeenLastCalledWith({
      index: -1,
      mistakes: 1,
      typed: "x",
      final: false,
    });
  });

  it("does not advance the index for a correct character after a mistake", () => {
    const { result } = renderTyping({ passage: "ab" });

    typeText(result, "xb");

    expect(onProgress).toHaveBeenLastCalledWith(
      expect.objectContaining({ index: -1, typed: "xb" }),
    );
  });

  it("rejects a space after a wrong word", () => {
    const { result } = renderTyping({ passage: "ab cd" });

    typeText(result, "ax");
    const callsBefore = onProgress.mock.calls.length;
    typeText(result, " ");

    expect(result.current.typed).toBe("ax");
    expect(onProgress).toHaveBeenCalledTimes(callsBefore);
  });

  it("accepts a space after a correct word", () => {
    const { result } = renderTyping({ passage: "ab cd" });

    typeText(result, "ab ");

    expect(result.current.typed).toBe("ab ");
    expect(onProgress).toHaveBeenLastCalledWith(
      expect.objectContaining({ index: 2, typed: "ab " }),
    );
  });

  it("stops accepting characters past the overflow limit", () => {
    const { result } = renderTyping({ passage: "abcdefghij" });

    typeText(result, "xxxxxxx");

    expect(result.current.typed).toBe("xxxxxx");
    expect(onProgress).toHaveBeenLastCalledWith(
      expect.objectContaining({ mistakes: 6 }),
    );
  });

  it("rolls the correct index back on backspace", () => {
    const { result } = renderTyping({ passage: "abc" });

    typeText(result, "ab");
    backspace(result);

    expect(result.current.typed).toBe("a");
    expect(onProgress).toHaveBeenLastCalledWith(
      expect.objectContaining({ index: 0, typed: "a" }),
    );
  });

  it("marks progress final and blurs the input on the last correct character", () => {
    const { result } = renderTyping();
    const input = document.createElement("input");
    const blur = vi.spyOn(input, "blur");

    typeText(result, "h");
    result.current.inputRef.current = input;
    typeText(result, "i");

    expect(blur).toHaveBeenCalledTimes(1);
    expect(onProgress).toHaveBeenLastCalledWith({
      index: 1,
      mistakes: 0,
      typed: "hi",
      final: true,
    });
  });

  it("ignores typing while disabled", () => {
    const { result } = renderTyping({ disabled: true });

    typeText(result, "h");

    expect(result.current.typed).toBe("");
    expect(onProgress).not.toHaveBeenCalled();
  });

  it("does not send progress before the first race state", () => {
    const { result } = renderTyping({
      selfTyped: undefined,
      selfCorrectIndex: undefined,
    });

    typeText(result, "h");

    expect(result.current.typed).toBe("h");
    expect(onProgress).not.toHaveBeenCalled();
  });

  it("sends the typed text once the first race state arrives", () => {
    const { result, rerender } = renderTyping({
      selfTyped: undefined,
      selfCorrectIndex: undefined,
    });
    typeText(result, "h");

    rerender(typingProps({ selfTyped: "", selfCorrectIndex: -1 }));

    expect(onProgress).toHaveBeenLastCalledWith({
      index: 0,
      mistakes: 0,
      typed: "h",
      final: false,
    });
  });

  it("restores progress from a longer server state", () => {
    const { result, rerender } = renderTyping({
      passage: "abc",
      selfTyped: undefined,
      selfCorrectIndex: undefined,
    });

    rerender(
      typingProps({
        passage: "abc",
        selfTyped: "ab",
        selfCorrectIndex: 1,
        selfMistakes: 2,
      }),
    );
    expect(result.current.typed).toBe("ab");

    const input = document.createElement("input");
    const blur = vi.spyOn(input, "blur");
    result.current.inputRef.current = input;
    typeText(result, "c");

    expect(blur).toHaveBeenCalledTimes(1);
    expect(onProgress).toHaveBeenLastCalledWith({
      index: 2,
      mistakes: 2,
      typed: "abc",
      final: true,
    });
  });

  it("does not send progress while disconnected", () => {
    const props = typingProps({ passage: "abc" });
    const { result, rerender } = renderTyping({ passage: "abc" });
    typeText(result, "a");
    expect(onProgress).toHaveBeenCalledTimes(1);

    setConnected(rerender, props, false);
    typeText(result, "b");

    expect(onProgress).toHaveBeenCalledTimes(1);
  });

  it("sends the typed text once reconnected", () => {
    const props = typingProps({ passage: "abc" });
    const { result, rerender } = renderTyping({ passage: "abc" });
    typeText(result, "a");
    setConnected(rerender, props, false);
    typeText(result, "b");

    setConnected(rerender, props, true);

    expect(onProgress).toHaveBeenLastCalledWith({
      index: 1,
      mistakes: 0,
      typed: "ab",
      final: false,
    });
  });

  it("keeps a backspace made while disconnected after reconnecting", () => {
    const { result, rerender } = renderTyping({ passage: "abc" });
    typeText(result, "ab");
    const props = typingProps({
      passage: "abc",
      selfTyped: "ab",
      selfCorrectIndex: 1,
    });
    rerender(props);
    setConnected(rerender, props, false);
    backspace(result);

    setConnected(rerender, props, true);

    expect(result.current.typed).toBe("a");
    expect(onProgress).toHaveBeenLastCalledWith({
      index: 0,
      mistakes: 0,
      typed: "a",
      final: false,
    });
  });
});
