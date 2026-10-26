import { vi, type Mock } from "vitest";
import type { IGameClient } from "../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.UseCases.Realtime.Ports";
import type {
  GameHub,
  GameHubServer,
  HubStatus,
} from "../features/connection/gameHub";

export type FakeGameHub = GameHub & {
  server: { [K in keyof GameHubServer]: Mock<GameHubServer[K]> };
  start: Mock<GameHub["start"]>;
  stop: Mock<GameHub["stop"]>;
  client(): IGameClient;
  setStatus(status: HubStatus): void;
};

export function fakeGameHub(): FakeGameHub {
  let receiver: IGameClient | null = null;
  let onStatus: ((status: HubStatus) => void) | null = null;

  const server: FakeGameHub["server"] = {
    connectToLobby: vi.fn(async () => undefined),
    refreshLobby: vi.fn(async () => undefined),
    startRace: vi.fn(async () => undefined),
    refreshPassage: vi.fn(async () => undefined),
    transferHost: vi.fn(async () => undefined),
    kickPlayer: vi.fn(async () => undefined),
    changeColor: vi.fn(async () => undefined),
    updateRaceState: vi.fn(async () => undefined),
    leaveLobby: vi.fn(async () => undefined),
  };

  return {
    server,
    start: vi.fn(async (r, status) => {
      receiver = r;
      onStatus = status;
      status("connected");
    }),
    stop: vi.fn(async () => undefined),
    client() {
      if (!receiver) throw new Error("The fake hub has not been started");
      return receiver;
    },
    setStatus(status) {
      onStatus?.(status);
    },
  };
}
