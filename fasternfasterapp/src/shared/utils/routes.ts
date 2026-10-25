export const ROUTES = {
  LOBBIES: "/lobbies",
  HOME: "/",
} as const;

export const lobbyPath = (lobbyId: string) => `/lobby/${lobbyId}`;
