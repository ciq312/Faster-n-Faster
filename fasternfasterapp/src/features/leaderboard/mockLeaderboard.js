// Client-side stand-in for GET /api/leaderboards. Mirrors the backend response
// shape so the hook can swap it in unchanged. Enable via VITE_MOCK_LEADERBOARD=true.

const TOTAL_PLAYERS = 47;

const SORT_FIELDS = {
  BestWpm: "bestWPM",
  AvgWpm: "avgWPM",
  BestAccuracy: "bestAccuracy",
  AvgAccuracy: "avgAccuracy",
  Wins: "wins",
  WordsTyped: "wordsTyped",
  RacesTyped: "racesTyped",
};

// Deterministic players so ranks stay stable across page flips.
const PLAYERS = Array.from({ length: TOTAL_PLAYERS }, (_, i) => {
  const n = i + 1;
  return {
    id: `mock-${n}`,
    playerName: `Player ${n}`,
    bestWPM: 60 + ((n * 7) % 90),
    bestAccuracy: 80 + ((n * 3) % 20),
    avgWPM: 40 + ((n * 5) % 70),
    avgAccuracy: 75 + ((n * 2) % 25),
    wins: (n * 13) % 40,
    wordsTyped: n * 250,
    racesTyped: (n * 3) % 60,
  };
});

export function getMockLeaderboard(sort, descending, page, pageSize) {
  const field = SORT_FIELDS[sort] ?? "bestWPM";

  const sorted = [...PLAYERS].sort((a, b) =>
    descending ? b[field] - a[field] : a[field] - b[field],
  );

  const totalPlayers = sorted.length;
  const totalPages = Math.max(1, Math.ceil(totalPlayers / pageSize));
  const start = (page - 1) * pageSize;

  const items = sorted.slice(start, start + pageSize).map((player, i) => ({
    ...player,
    rank: start + i + 1,
  }));

  return { items, page, pageSize, totalPlayers, totalPages };
}
