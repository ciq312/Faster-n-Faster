import { describe, expect, it } from "vitest";
import { setupStore } from "../../app/store";
import { fakeGameHub } from "../../test/fakeGameHub";
import { lobbyState } from "../../test/fixtures";
import { signIn } from "../../test/store";
import {
  connectHub,
  connectToLobby,
  disconnectHub,
  leaveLobby,
} from "../connection/hubThunks";
import {
  selectIsHost,
  selectJoinedLobby,
  selectLobbyId,
  selectWasKicked,
} from "./lobbySelectors";

async function setup() {
  const hub = fakeGameHub();
  const store = setupStore({ hub });
  await store.dispatch(connectHub());
  return { hub, store };
}

describe("lobby selectors", () => {
  it("hides a lobby state that arrives before the current user is known", async () => {
    const { hub, store } = await setup();

    await hub.client().lobbyState(lobbyState());

    expect(selectJoinedLobby(store.getState())).toBeNull();
  });

  it("shows a lobby state that arrived before the current user once the user is known", async () => {
    const { hub, store } = await setup();
    await hub.client().lobbyState(lobbyState());

    await signIn(store);

    expect(selectJoinedLobby(store.getState())).toEqual(lobbyState());
  });

  it("shows a lobby state that arrives after the current user is known", async () => {
    const { hub, store } = await setup();
    await signIn(store);

    await hub.client().lobbyState(lobbyState());

    expect(selectJoinedLobby(store.getState())).toEqual(lobbyState());
  });

  it("hides a lobby state that does not include the current user", async () => {
    const { hub, store } = await setup();
    await signIn(store, { userId: "p9", userName: "Stranger", role: "User" });

    await hub.client().lobbyState(lobbyState());

    expect(selectJoinedLobby(store.getState())).toBeNull();
  });

  it("reports the current user as host", async () => {
    const { hub, store } = await setup();
    await signIn(store);
    await hub.client().lobbyState(lobbyState());

    expect(selectIsHost(store.getState())).toBe(true);
  });

  it("reports a non-host current user as not host", async () => {
    const { hub, store } = await setup();
    await signIn(store, { userId: "p2", userName: "Other", role: "User" });
    await hub.client().lobbyState(lobbyState());

    expect(selectIsHost(store.getState())).toBe(false);
  });

  it("gives the joined lobby's id", async () => {
    const { hub, store } = await setup();
    await signIn(store);
    await hub.client().lobbyState(lobbyState({ lobbyId: "l7" }));

    expect(selectLobbyId(store.getState())).toBe("l7");
  });

  it("resets the lobby and flags it when the current user is kicked", async () => {
    const { hub, store } = await setup();
    await signIn(store);
    await hub.client().lobbyState(lobbyState());

    await hub.client().kicked();

    expect(selectJoinedLobby(store.getState())).toBeNull();
    expect(selectWasKicked(store.getState())).toBe(true);
  });

  it("resets the lobby after leaving", async () => {
    const { hub, store } = await setup();
    await signIn(store);
    await hub.client().lobbyState(lobbyState());

    await store.dispatch(leaveLobby());

    expect(selectJoinedLobby(store.getState())).toBeNull();
  });

  it("keeps the lobby when leaving fails", async () => {
    const { hub, store } = await setup();
    await signIn(store);
    await hub.client().lobbyState(lobbyState());
    hub.server.leaveLobby.mockRejectedValueOnce(new Error("socket closed"));

    await store.dispatch(leaveLobby());

    expect(selectJoinedLobby(store.getState())).not.toBeNull();
  });

  it("resets the lobby when the hub disconnects", async () => {
    const { hub, store } = await setup();
    await signIn(store);
    await hub.client().lobbyState(lobbyState());

    await store.dispatch(disconnectHub());

    expect(selectJoinedLobby(store.getState())).toBeNull();
  });

  it("clears the kicked flag when joining another lobby", async () => {
    const { hub, store } = await setup();
    await hub.client().kicked();

    await store.dispatch(connectToLobby({ lobbyId: "l2" }));

    expect(selectWasKicked(store.getState())).toBe(false);
  });
});
