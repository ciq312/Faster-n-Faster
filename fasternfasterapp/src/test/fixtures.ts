import type { LobbyStateDTO } from "../../generated/signalr/FasterNFaster.Api.UseCases.Lobbies.LobbyState";

export function lobbyState(
  overrides: Partial<LobbyStateDTO> = {},
): LobbyStateDTO {
  return {
    lobbyId: "l1",
    lobbyName: "Lobby",
    isRaceStarting: false,
    isRaceStarted: false,
    settings: { passage: "hi there", wordCount: 2 },
    isPrivate: false,
    inviteCode: "ABC123",
    maxPlayers: 4,
    colors: [
      { color: "red", isAvailable: true },
      { color: "blue", isAvailable: true },
    ],
    players: [
      {
        id: "p1",
        isHost: true,
        nick: "Player",
        joinOrder: 0,
        isConnected: true,
        color: "red",
      },
      {
        id: "p2",
        isHost: false,
        nick: "Other",
        joinOrder: 1,
        isConnected: true,
        color: "blue",
      },
    ],
    ...overrides,
  };
}
