import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useAppSelector } from "../../../app/hooks";
import { setupStore } from "../../../app/store";
import { fakeGameHub } from "../../../test/fakeGameHub";
import { lobbyState } from "../../../test/fixtures";
import { signIn, signOut, wrapperFor } from "../../../test/store";
import { connectHub } from "../../connection/hubThunks";
import { selectJoinedLobby } from "../lobbySelectors";
import { useLobbyRefresh } from "./useLobbyRefresh";

async function setup() {
  const hub = fakeGameHub();
  hub.server.refreshLobby.mockImplementation(async () => {
    await hub.client().lobbyState(lobbyState());
  });
  const store = setupStore({ hub });
  await store.dispatch(connectHub());
  const hook = renderHook(
    () => {
      useLobbyRefresh();
      return useAppSelector(selectJoinedLobby);
    },
    { wrapper: wrapperFor(store) },
  );
  return { hub, store, ...hook };
}

describe("useLobbyRefresh", () => {
  it("renders the lobby when auth resolves after the hub connects", async () => {
    const { store, result } = await setup();

    await act(() => signIn(store));

    await vi.waitFor(() => expect(result.current).toEqual(lobbyState()));
  });

  it("does not invoke RefreshLobby while auth is loading", async () => {
    const { hub } = await setup();

    expect(hub.server.refreshLobby).not.toHaveBeenCalled();
  });

  it("invokes RefreshLobby once after auth resolves", async () => {
    const { hub, store, rerender } = await setup();

    await act(() => signIn(store));
    rerender();
    rerender();

    expect(hub.server.refreshLobby).toHaveBeenCalledTimes(1);
  });

  it("does not show the lobby state while not authenticated", async () => {
    const { hub, store, result } = await setup();
    await act(() => signOut(store));

    await act(() => hub.client().lobbyState(lobbyState()));

    expect(result.current).toBeNull();
    expect(hub.server.refreshLobby).not.toHaveBeenCalled();
  });
});
