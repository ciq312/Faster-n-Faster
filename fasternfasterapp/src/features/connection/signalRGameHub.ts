import {
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from "@microsoft/signalr";
import {
  getHubProxyFactory,
  getReceiverRegister,
} from "../../../generated/signalr/TypedSignalR.Client";
import type { IGameHub } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.Presentation.Web.Realtime";
import type { GameHub, GameHubServer, HubStatus } from "./gameHub";

const LATENCY_CHECK_MS = 3000;

export function createSignalRGameHub(url: string): GameHub {
  let connection: HubConnection | null = null;
  let proxy: IGameHub | null = null;
  let latencyCheckId: ReturnType<typeof setInterval> | undefined;

  function startLatencyCheck(started: HubConnection, hubProxy: IGameHub) {
    latencyCheckId = setInterval(async () => {
      if (started.state !== HubConnectionState.Connected) return;
      try {
        const t0 = performance.now();
        await hubProxy.ping(Date.now());
        console.debug(`latency: ${Math.round(performance.now() - t0)}ms`);
      } catch {
        console.log("latency check failed");
      }
    }, LATENCY_CHECK_MS);
  }

  return {
    get server() {
      if (!proxy) throw new Error("The game hub is not connected");
      // The generated proxy types inviteCode as string, but the server accepts null.
      return proxy as unknown as GameHubServer;
    },

    async start(receiver, onStatus) {
      if (connection) throw new Error("The game hub is already started");

      const started = new HubConnectionBuilder()
        .withUrl(url, { withCredentials: true })
        .withAutomaticReconnect()
        .build();
      const hubProxy = getHubProxyFactory("IGameHub").createHubProxy(started);
      connection = started;
      proxy = hubProxy;
      getReceiverRegister("IGameClient").register(started, receiver);

      const report = (status: HubStatus) => {
        if (connection === started) onStatus(status);
      };
      started.onreconnecting(() => report("reconnecting"));
      started.onreconnected(() => report("connected"));
      started.onclose(() => report("disconnected"));

      try {
        await started.start();
      } catch (e) {
        if (connection !== started) return;
        connection = null;
        proxy = null;
        throw e;
      }
      if (connection !== started) return;

      report("connected");
      if (import.meta.env.DEV) startLatencyCheck(started, hubProxy);
    },

    async stop() {
      const stopping = connection;
      connection = null;
      proxy = null;
      clearInterval(latencyCheckId);
      await stopping?.stop();
    },
  };
}
