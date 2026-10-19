import { useLobby } from "./useLobby";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook } from "@testing-library/react";
import { useAuth } from "../../auth/AuthContext";
import { useConnection } from "../../connection/ConnectionProvider";

vi.mock("../../auth/AuthContext", () => ({
  useAuth: vi.fn(),
}));
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

  beforeEach(() => {
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

  function authAs(status) {
    vi.mocked(useAuth).mockReturnValue({
      status,
      isSelf: (id) => status === "authenticated" && id === "p1",
    });
  }

  it("renders the lobby when auth resolves after the hub connects", async () => {
    authAs("loading");
    const { result, rerender } = renderHook(() => useLobby());

    authAs("authenticated");
    rerender();

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
    authAs("loading");

    renderHook(() => useLobby());

    expect(invoke).not.toHaveBeenCalled();
  });

  it("invokes RefreshLobby once after auth resolves", () => {
    authAs("loading");
    const { rerender } = renderHook(() => useLobby());
    invoke.mockClear();

    authAs("authenticated");
    rerender();
    rerender();

    expect(invoke).toHaveBeenCalledTimes(1);
    expect(invoke).toHaveBeenCalledWith("RefreshLobby");
  });

  it("does not subscribe when not authenticated", () => {
    authAs("not authenticated");

    renderHook(() => useLobby());

    expect(subscribe).not.toHaveBeenCalled();
  });
});
