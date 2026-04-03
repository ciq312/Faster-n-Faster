import { useState, useEffect, useCallback } from "react";
import { extractError } from "../../../shared/utils/extractError";

export function useFetchLeaderboard() {
  const [players, setPlayers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [criteria, setCriteria] = useState("BestWPM");
  const [isDescending, setIsDescending] = useState(true);
  const [playersCount, setPlayersCount] = useState(5);

  const fetchLeaderboard = useCallback(async (crit, desc, count) => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch("/api/leaderboards", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          criteria: crit,
          isDescending: desc,
          playersCount: count,
        }),
      });
      if (!response.ok) {
        setError(await extractError(response));
        return;
      }
      const data = await response.json();
      setPlayers(data.results);
    } catch {
      setError("Could not connect to server");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLeaderboard(criteria, isDescending, playersCount);
  }, [criteria, isDescending, playersCount, fetchLeaderboard]);

  const sortBy = useCallback(
    (newCriteria) => {
      if (newCriteria === criteria) {
        setIsDescending((prev) => !prev);
      } else {
        setCriteria(newCriteria);
        setIsDescending(true);
      }
    },
    [criteria],
  );

  return {
    players,
    loading,
    error,
    setError,
    criteria,
    isDescending,
    playersCount,
    setPlayersCount,
    sortBy,
  };
}
