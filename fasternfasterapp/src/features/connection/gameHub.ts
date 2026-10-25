import type { IGameClient } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.UseCases.Realtime.Ports";
import type { IGameHub } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.Presentation.Web.Realtime";

export type HubStatus = "connected" | "reconnecting" | "disconnected";

export type GameHubServer = Omit<IGameHub, "connectToLobby" | "ping"> & {
  connectToLobby(lobbyId: string, inviteCode: string | null): Promise<void>;
};

export type GameHub = {
  readonly server: GameHubServer;
  start(
    receiver: IGameClient,
    onStatus: (status: HubStatus) => void,
  ): Promise<void>;
  stop(): Promise<void>;
};
