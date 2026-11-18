import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IGameClient } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.UseCases.Realtime.Ports";
import { lobbyState } from "../../test/fixtures";
import type { GameHub, HubStatus } from "./gameHub";
import { createSignalRGameHub } from "./signalRGameHub";

const fake = vi.hoisted(() => {
  const connections: FakeConnection[] = [];
  const behavior = {
    start: (): Promise<void> => Promise.resolve(),
  };

  class FakeConnection {
    state = "Connected";
    handlers = new Map<string, (...args: unknown[]) => void>();
    reconnecting: () => void = () => {};
    reconnected: () => void = () => {};
    closed: () => void = () => {};
    start = vi.fn(() => behavior.start());
    stop = vi.fn(async () => {});
    invoke = vi.fn(async () => {});
    on = vi.fn((name: string, handler: (...args: unknown[]) => void) => {
      this.handlers.set(name, handler);
    });
    off = vi.fn();
    onreconnecting = vi.fn((cb: () => void) => {
      this.reconnecting = cb;
    });
    onreconnected = vi.fn((cb: () => void) => {
      this.reconnected = cb;
    });
    onclose = vi.fn((cb: () => void) => {
      this.closed = cb;
    });
  }

  class FakeBuilder {
    url = "";
    withUrl(url: string) {
      this.url = url;
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    build() {
      const connection = new FakeConnection();
      connections.push(connection);
      return connection;
    }
  }

  return { connections, behavior, FakeBuilder };
});

vi.mock("@microsoft/signalr", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@microsoft/signalr")>()),
  HubConnectionBuilder: fake.FakeBuilder,
}));

function latestConnection() {
  return fake.connections[fake.connections.length - 1];
}

describe("createSignalRGameHub", () => {
  let hub: GameHub;
  let statuses: HubStatus[];
  let receiver: { lobbyState: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    fake.connections.length = 0;
    fake.behavior.start = () => Promise.resolve();
    hub = createSignalRGameHub("http://hub");
    statuses = [];
    receiver = { lobbyState: vi.fn() };
  });

  afterEach(async () => {
    await hub.stop();
  });

  function startHub() {
    return hub.start(receiver as unknown as IGameClient, (s) =>
      statuses.push(s),
    );
  }

  it("reports connected once the connection starts", async () => {
    await startHub();

    expect(statuses).toEqual(["connected"]);
  });

  it("ignores the abort when stopped while starting", async () => {
    fake.behavior.start = () =>
      new Promise<void>((_, reject) => {
        queueMicrotask(() => reject(new Error("AbortError")));
      });
    const starting = startHub();

    await hub.stop();

    await expect(starting).resolves.toBeUndefined();
    expect(statuses).toEqual([]);
  });

  it("rejects when the connection fails to start", async () => {
    fake.behavior.start = () => Promise.reject(new Error("unreachable"));

    await expect(startHub()).rejects.toThrow("unreachable");
    expect(statuses).toEqual([]);
  });

  it("is not connected after a failed start", async () => {
    fake.behavior.start = () => Promise.reject(new Error("unreachable"));
    await startHub().catch(() => {});

    expect(() => hub.server).toThrow("The game hub is not connected");
  });

  it("keeps the new connection when an earlier start fails after a restart", async () => {
    let failFirstStart!: (error: Error) => void;
    fake.behavior.start = () =>
      new Promise<void>((_, reject) => {
        failFirstStart = reject;
      });
    const firstStart = startHub();
    await hub.stop();
    fake.behavior.start = () => Promise.resolve();
    await startHub();

    failFirstStart(new Error("AbortError"));
    await firstStart;

    expect(statuses).toEqual(["connected"]);
    expect(() => hub.server).not.toThrow();
  });

  it("rejects a second start while connected", async () => {
    await startHub();

    await expect(startHub()).rejects.toThrow("already started");
  });

  it("reports reconnecting and reconnected", async () => {
    await startHub();
    const connection = latestConnection();

    connection.reconnecting();
    connection.reconnected();
    connection.closed();

    expect(statuses).toEqual([
      "connected",
      "reconnecting",
      "connected",
      "disconnected",
    ]);
  });

  it("ignores status changes from a stopped connection", async () => {
    await startHub();
    const connection = latestConnection();

    await hub.stop();
    connection.closed();
    connection.reconnecting();

    expect(statuses).toEqual(["connected"]);
  });

  it("forwards server events to the receiver", async () => {
    await startHub();
    const state = lobbyState();

    latestConnection().handlers.get("LobbyState")?.(state);

    expect(receiver.lobbyState).toHaveBeenCalledWith(state);
  });

  it("invokes hub methods on the started connection", async () => {
    await startHub();

    await hub.server.changeColor("red");

    expect(latestConnection().invoke).toHaveBeenCalledWith(
      "ChangeColor",
      "red",
    );
  });

  it("passes a missing invite code as null", async () => {
    await startHub();

    await hub.server.connectToLobby("l1", null);

    expect(latestConnection().invoke).toHaveBeenCalledWith(
      "ConnectToLobby",
      "l1",
      null,
    );
  });

  it("throws on a hub call before the hub starts", () => {
    expect(() => hub.server).toThrow("The game hub is not connected");
  });
});
