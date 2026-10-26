import type { RaceResultDTO } from "../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceFinished";
import type { RaceParticipantDTO } from "../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceState";
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

export function raceResult(
  overrides: Partial<RaceResultDTO> = {},
): RaceResultDTO {
  return {
    playerId: "p1",
    nick: "Player",
    finishPosition: 1,
    wpm: 40,
    accuracy: 100,
    mistakeCount: 0,
    ...overrides,
  };
}

export function raceParticipant(
  overrides: Partial<RaceParticipantDTO> = {},
): RaceParticipantDTO {
  return {
    playerId: "p1",
    index: 2,
    typed: "hi ",
    wpm: 40,
    color: "red",
    nick: "Player",
    mistakes: 1,
    ...overrides,
  };
}
