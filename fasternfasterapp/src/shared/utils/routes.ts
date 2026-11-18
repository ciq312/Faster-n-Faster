export const ROUTES = {
  LOBBIES: "/lobbies",
  HOME: "/",
  CHOOSE_NICK: "/choose-nick",
} as const;

export const lobbyPath = (lobbyId: string) => `/lobby/${lobbyId}`;
