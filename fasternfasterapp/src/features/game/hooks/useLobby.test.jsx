import { useLobby } from "./useLobby";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { act, renderHook } from "@testing-library/react";
import { Provider } from "react-redux";
import { setupStore } from "../../../app/store";
import { signedOut } from "../../../test/handlers";
import { server } from "../../../test/server";
import { authApi } from "../../auth/authApi";
import { useConnection } from "../../connection/ConnectionProvider";

vi.mock("../../connection/ConnectionProvider", () => ({
  useConnection: vi.fn(),
}));

const lobbyState = {
  players: [
    { id: "p1", isHost: true },
    { id: "p2", isHost: false },
  ],
  lobbyName: "Lobby",
  maxPlayers: 4,
  inviteCode: "ABC123",
  colors: ["red", "blue"],
};

describe("useLobby", () => {
  let invoke;
  let subscribe;
  let store;

  const wrapper = ({ children }) => (
    <Provider store={store}>{children}</Provider>
  );

  beforeEach(() => {
    store = setupStore();
    const handlers = {};
    subscribe = vi.fn((name, cb) => {
      handlers[name] = cb;
      return () => delete handlers[name];
    });
    invoke = vi.fn(async (method) => {
      if (method === "RefreshLobby") handlers.LobbyState?.(lobbyState);
    });
    vi.mocked(useConnection).mockReturnValue({
      invoke,
      subscribe,
      isConnected: true,
    });
  });

  async function authAs(status) {
    if (status === "authenticated") {
      await act(() =>
        store.dispatch(
          authApi.util.upsertQueryData("me", undefined, {
            userId: "p1",
            userName: "Player",
            role: "User",
          }),
        ),
      );
    }
    if (status === "not authenticated") {
      server.use(...signedOut());
      await act(() => store.dispatch(authApi.endpoints.me.initiate()));
    }
  }

  it("renders the lobby when auth resolves after the hub connects", async () => {
    const { result } = renderHook(() => useLobby(), { wrapper });

    await authAs("authenticated");

    await vi.waitFor(() =>
      expect(result.current.players).toEqual(lobbyState.players),
    );
    expect(result.current).toEqual({
      isHost: true,
      players: lobbyState.players,
      lobbyName: "Lobby",
      lobbyMaxPlayers: 4,
      lobbyInviteCode: "ABC123",
      colors: lobbyState.colors,
    });
  });

  it("does not invoke RefreshLobby while auth is loading", () => {
    renderHook(() => useLobby(), { wrapper });

    expect(invoke).not.toHaveBeenCalled();
  });

  it("invokes RefreshLobby once after auth resolves", async () => {
    const { rerender } = renderHook(() => useLobby(), { wrapper });
    invoke.mockClear();

    await authAs("authenticated");
    rerender();
    rerender();

    expect(invoke).toHaveBeenCalledTimes(1);
    expect(invoke).toHaveBeenCalledWith("RefreshLobby");
  });

  it("does not subscribe when not authenticated", async () => {
    await authAs("not authenticated");

    renderHook(() => useLobby(), { wrapper });

    expect(subscribe).not.toHaveBeenCalled();
  });
});
