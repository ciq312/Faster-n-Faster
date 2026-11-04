import type { RaceProgressDTO } from "../../../generated/signalr/FasterNFaster.Api.UseCases.Races.GetRaceProgress";
import type { IGameClient } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.UseCases.Realtime.Ports";
import type { IGameHub } from "../../../generated/signalr/TypedSignalR.Client/FasterNFaster.Api.Presentation.Web.Realtime";

export type HubStatus = "connected" | "reconnecting" | "disconnected";

export type GameHubServer = Omit<
  IGameHub,
  "connectToLobby" | "getRaceProgress" | "ping"
> & {
  connectToLobby(lobbyId: string, inviteCode: string | null): Promise<void>;
  getRaceProgress(): Promise<RaceProgressDTO | null>;
};

export type GameHub = {
  readonly server: GameHubServer;
  start(
    receiver: IGameClient,
    onStatus: (status: HubStatus) => void,
  ): Promise<void>;
  stop(): Promise<void>;
};
