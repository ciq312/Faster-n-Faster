const PLAYER_ID_KEY = "playerId";
const DISPLAY_NAME_KEY = "displayName";

export function getPlayer() {
  const playerId = localStorage.getItem(PLAYER_ID_KEY);
  const displayName = localStorage.getItem(DISPLAY_NAME_KEY);

  if (!playerId || !displayName) return null;
  return { playerId, displayName };
}

export function createAnonymousPlayer() {
  const playerId = crypto.randomUUID();
  const displayName = `Player_${playerId.substring(0, 4)}`;

  localStorage.setItem(PLAYER_ID_KEY, playerId);
  localStorage.setItem(DISPLAY_NAME_KEY, displayName);

  return { playerId, displayName };
}

export function isLoggedIn() {
  return localStorage.getItem(PLAYER_ID_KEY) !== null;
}
