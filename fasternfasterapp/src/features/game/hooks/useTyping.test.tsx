import { act, renderHook } from "@testing-library/react";
import type { ChangeEvent } from "react";
import { beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import type { RaceProgressDTO } from "../../../../generated/signalr/FasterNFaster.Api.UseCases.Races.GetRaceProgress";
import { setupStore } from "../../../app/store";
import { deferred } from "../../../test/deferred";
import { fakeGameHub, type FakeGameHub } from "../../../test/fakeGameHub";
import { wrapperFor } from "../../../test/store";
import { hubStatusChanged } from "../../connection/hubThunks";
import { useTyping, type TypingProgress } from "./useTyping";

type TypingOptions = Parameters<typeof useTyping>[0];

describe("useTyping", () => {
  let hub: FakeGameHub;
  let store: ReturnType<typeof setupStore>;
  let onProgress: Mock<(progress: TypingProgress) => void>;

  beforeEach(() => {
    hub = fakeGameHub();
    store = setupStore({ hub });
    store.dispatch(hubStatusChanged("connected"));
    onProgress = vi.fn();
  });

  function typingProps(overrides?: Partial<TypingOptions>): TypingOptions {
    return { passage: "ab", disabled: false, onProgress, ...overrides };
  }

  // The resync answers through a thunk; a macrotask lets its whole promise chain finish.
  async function settle() {
    await act(() => new Promise((resolve) => setTimeout(resolve)));
  }

  async function renderTyping(overrides?: Partial<TypingOptions>) {
    const rendered = renderHook((props: TypingOptions) => useTyping(props), {
      initialProps: typingProps(overrides),
      wrapper: wrapperFor(store),
    });
    await settle();
    return rendered;
  }

  function setConnected(isConnected: boolean) {
    act(() => {
      store.dispatch(
        hubStatusChanged(isConnected ? "connected" : "disconnected"),
      );
    });
  }

  type Typing = Awaited<ReturnType<typeof renderTyping>>["result"];

  // handleTyping reads only the input's new value.
  function changeTo(value: string) {
    return { target: { value } } as ChangeEvent<HTMLInputElement>;
  }

  function typeText(result: Typing, text: string) {
    for (const ch of text) {
      act(() =>
        result.current.handleTyping(changeTo(result.current.typed + ch)),
      );
    }
  }

  function backspace(result: Typing) {
    act(() =>
      result.current.handleTyping(changeTo(result.current.typed.slice(0, -1))),
    );
  }

  // final is true only once the valid index reaches the passage's last character.
  function sent(key: string, final = false) {
    return [{ keystroke: { key }, final }];
  }

  it("drops a character past the overflow limit", async () => {
    const { result } = await renderTyping({ passage: "abcdefghij" });

    typeText(result, "xxxxxxx");

    expect(result.current.typed).toBe("xxxxxx");
    expect(onProgress).toHaveBeenCalledTimes(6);
  });

  it("drops a character past the end of the passage", async () => {
    const { result } = await renderTyping({ passage: "ab" });

    typeText(result, "axy");

    expect(result.current.typed).toBe("ax");
    expect(onProgress).toHaveBeenCalledTimes(2);
  });

  it("drops a space after a wrong character", async () => {
    const { result } = await renderTyping({ passage: "ab cd" });

    typeText(result, "ax ");

    expect(result.current.typed).toBe("ax");
    expect(onProgress).toHaveBeenCalledTimes(2);
  });

  it("advances the valid index for each correct character", async () => {
    const { result } = await renderTyping({ passage: "ab" });

    typeText(result, "ab");

    expect(result.current.typed).toBe("ab");
    expect(onProgress.mock.calls).toEqual([sent("a"), sent("b", true)]);
  });

  it("keeps the valid index after a wrong character", async () => {
    const { result } = await renderTyping({ passage: "ab" });

    typeText(result, "xb");

    expect(result.current.typed).toBe("xb");
    expect(onProgress.mock.calls).toEqual([sent("x"), sent("b")]);
  });

  it("moves the valid index back when deleting a correct character", async () => {
    const { result } = await renderTyping({ passage: "ab" });
    typeText(result, "ab");

    backspace(result);

    expect(result.current.typed).toBe("a");
    expect(onProgress.mock.calls).toEqual([
      sent("a"),
      sent("b", true),
      sent("Backspace"),
    ]);
  });

  it("keeps the valid index when deleting a wrong character", async () => {
    const { result } = await renderTyping({ passage: "ab" });
    typeText(result, "ax");

    backspace(result);
    typeText(result, "b");

    expect(result.current.typed).toBe("ab");
    expect(onProgress.mock.calls).toEqual([
      sent("a"),
      sent("x"),
      sent("Backspace"),
      sent("b", true),
    ]);
  });

  it("focuses the input once typing is enabled", async () => {
    const { result, rerender } = await renderTyping({ disabled: true });
    const input = document.createElement("input");
    const focus = vi.spyOn(input, "focus");
    result.current.inputRef.current = input;

    rerender(typingProps({ disabled: false }));

    expect(focus).toHaveBeenCalledTimes(1);
  });

  it("fetches the server's progress once connected", async () => {
    await renderTyping();

    expect(hub.server.getRaceProgress).toHaveBeenCalledTimes(1);
  });

  it("does not fetch the progress again while it stays connected", async () => {
    const { rerender } = await renderTyping();

    rerender(typingProps({ passage: "cd" }));
    await settle();

    expect(hub.server.getRaceProgress).toHaveBeenCalledTimes(1);
  });

  it("drops typing while disconnected", async () => {
    const { result } = await renderTyping();
    setConnected(false);

    typeText(result, "a");

    expect(result.current.typed).toBe("");
    expect(onProgress).not.toHaveBeenCalled();
  });

  it("drops typing after reconnecting until the server answers", async () => {
    const { result } = await renderTyping();
    const answer = deferred<RaceProgressDTO | null>();
    hub.server.getRaceProgress.mockReturnValueOnce(answer.promise);
    setConnected(false);
    setConnected(true);

    typeText(result, "a");
    expect(result.current.typed).toBe("");

    answer.resolve(null);
    await settle();
    typeText(result, "a");

    expect(result.current.typed).toBe("a");
    expect(onProgress.mock.calls).toEqual([sent("a")]);
  });

  it("restores the typed text and the valid index from the server after reconnecting", async () => {
    const { result } = await renderTyping({ passage: "abcd" });
    typeText(result, "a");
    hub.server.getRaceProgress.mockResolvedValueOnce({
      lastValidIndex: 2,
      wrongTail: "x",
      mistakes: 1,
    });

    setConnected(false);
    setConnected(true);
    await settle();

    expect(result.current.typed).toBe("abcx");
    backspace(result);
    typeText(result, "d");
    expect(onProgress).toHaveBeenLastCalledWith(...sent("d", true));
  });
});
