import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractHttpError } from "../../../shared/utils/extractHttpError";

export function useFetchLeaderboard() {
  const navigate = useNavigate();
  const { showError } = useError();
  const [players, setPlayers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [criteria, setCriteria] = useState("BestWPM");
  const [isDescending, setIsDescending] = useState(true);
  const [playersCount, setPlayersCount] = useState(5);

  const fetchLeaderboard = useCallback(
    async (crit, desc, count) => {
      setLoading(true);
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
          showError(await extractHttpError(response));
          return;
        }
        const data = await response.json();
        await new Promise((res) => setTimeout(() => res(), 300));

        setPlayers(data.results);
      } catch {
        showError("Could not connect to server");
      } finally {
        setLoading(false);
      }
    },
    [showError],
  );

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
    criteria,
    isDescending,
    playersCount,
    setPlayersCount,
    sortBy,
  };
}
