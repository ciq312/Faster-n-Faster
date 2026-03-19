const BASE_URL = "/api/lobbies";

export async function getLobbies() {
  const res = await fetch(BASE_URL);
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  return data.lobbies;
}

export async function createLobby({ lobbyName, displayName, playerId, gameMode, isPrivate, wordCount, timerDurationSeconds }) {
  const res = await fetch(BASE_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ lobbyName, displayName, playerId, gameMode, isPrivate, wordCount, timerDurationSeconds }),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function joinLobby(lobbyId, { displayName, playerId }) {
  const res = await fetch(`${BASE_URL}/${lobbyId}/join`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ displayName, playerId }),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
