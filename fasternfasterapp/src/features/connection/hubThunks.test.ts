import { describe, expect, it, vi } from "vitest";
import { setupStore } from "../../app/store";
import { selectCurrentBanner } from "../../shared/banner/bannerSlice";
import { deferred } from "../../test/deferred";
import { fakeGameHub } from "../../test/fakeGameHub";
import {
  changeColor,
  connectHub,
  connectToLobby,
  disconnectHub,
  kickPlayer,
  leaveLobby,
  refreshLobby,
  refreshPassage,
  startRace,
  transferHost,
  updateRaceState,
} from "./hubThunks";

function setup() {
  const hub = fakeGameHub();
  const store = setupStore({ hub });
  return { hub, store };
}

type Store = ReturnType<typeof setup>["store"];

const bannerMessage = (store: Store) =>
  selectCurrentBanner(store.getState())?.message;

describe("hub thunks", () => {
  it("reports connecting while the hub starts and connected once it has", async () => {
    const { hub, store } = setup();
    const gate = deferred();
    hub.start.mockImplementationOnce(async (_, onStatus) => {
      await gate.promise;
      onStatus("connected");
    });

    const connecting = store.dispatch(connectHub());

    expect(store.getState().connection.status).toBe("connecting");
    gate.resolve();
    await connecting;
    expect(store.getState().connection.status).toBe("connected");
  });

  it("shows Can't connect to the server when the hub fails to start", async () => {
    const { hub, store } = setup();
    hub.start.mockRejectedValueOnce(new Error("unreachable"));

    await store.dispatch(connectHub());

    await vi.waitFor(() =>
      expect(bannerMessage(store)).toBe("Can't connect to the server"),
    );
    expect(store.getState().connection.status).toBe("disconnected");
  });

  it.each([
    {
      method: "connectToLobby",
      run: (store: Store) =>
        store.dispatch(connectToLobby({ lobbyId: "l1", inviteCode: "CODE" })),
      args: ["l1", "CODE"],
    },
    {
      method: "connectToLobby",
      run: (store: Store) => store.dispatch(connectToLobby({ lobbyId: "l1" })),
      args: ["l1", null],
    },
    {
      method: "leaveLobby",
      run: (store: Store) => store.dispatch(leaveLobby()),
      args: [],
    },
    {
      method: "refreshLobby",
      run: (store: Store) => store.dispatch(refreshLobby()),
      args: [],
    },
    {
      method: "changeColor",
      run: (store: Store) => store.dispatch(changeColor("red")),
      args: ["red"],
    },
    {
      method: "kickPlayer",
      run: (store: Store) => store.dispatch(kickPlayer("p2")),
      args: ["p2"],
    },
    {
      method: "transferHost",
      run: (store: Store) => store.dispatch(transferHost("p2")),
      args: ["p2"],
    },
    {
      method: "startRace",
      run: (store: Store) => store.dispatch(startRace()),
      args: [],
    },
    {
      method: "refreshPassage",
      run: (store: Store) => store.dispatch(refreshPassage()),
      args: [],
    },
    {
      method: "updateRaceState",
      run: (store: Store) =>
        store.dispatch(
          updateRaceState({ index: 3, mistakes: 1, typed: "abc" }),
        ),
      args: [3, 1, "abc"],
    },
  ] as const)(
    "invokes $method with the thunk arguments",
    async ({ method, run, args }) => {
      const { hub, store } = setup();

      await run(store);

      expect(hub.server[method]).toHaveBeenCalledWith(...args);
    },
  );

  it("shows the hub error in the banner when a hub call fails", async () => {
    const { hub, store } = setup();
    hub.server.connectToLobby.mockRejectedValueOnce(
      new Error("An unexpected error occurred. HubException: Lobby is full"),
    );

    await store.dispatch(connectToLobby({ lobbyId: "l1" }));

    await vi.waitFor(() => expect(bannerMessage(store)).toBe("Lobby is full"));
  });

  it("shows the fallback message for an unknown hub error", async () => {
    const { hub, store } = setup();
    hub.server.leaveLobby.mockRejectedValueOnce(new Error("socket closed"));

    await store.dispatch(leaveLobby());

    await vi.waitFor(() =>
      expect(bannerMessage(store)).toBe("Something went wrong, try again"),
    );
  });

  it("does not show a banner when a progress update fails", async () => {
    const { hub, store } = setup();
    hub.server.updateRaceState.mockRejectedValueOnce(
      new Error("HubException: Race is over"),
    );

    await store.dispatch(updateRaceState({ index: 0, mistakes: 0, typed: "" }));

    expect(bannerMessage(store)).toBeUndefined();
  });

  it("disconnects by stopping the hub", async () => {
    const { hub, store } = setup();

    await store.dispatch(disconnectHub());

    expect(hub.stop).toHaveBeenCalledTimes(1);
  });
});
