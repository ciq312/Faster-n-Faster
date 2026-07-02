import { useCallback, useEffect, useState } from "react";
import { useError } from "../../../shared/components/BannerProvider";
import { API_BASE } from "../../../shared/utils/apiCall";
import { extractHttpError } from "../../../shared/utils/extractHttpError";
import { getMockLeaderboard } from "../mockLeaderboard";

const PAGE_SIZE = 10;
const USE_MOCK = import.meta.env.VITE_MOCK_LEADERBOARD === "true";

export function useFetchLeaderboard() {
  const { showError } = useError();
  const [players, setPlayers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [criteria, setCriteria] = useState("BestWpm");
  const [isDescending, setIsDescending] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalPlayers, setTotalPlayers] = useState(0);

  const fetchLeaderboard = useCallback(
    async (sort, desc, pageNumber) => {
      setLoading(true);
      try {
        let data;
        if (USE_MOCK) {
          await new Promise((resolve) => setTimeout(resolve, 250)); // surface the loading state
          data = getMockLeaderboard(sort, desc, pageNumber, PAGE_SIZE);
        } else {
          const query = new URLSearchParams({
            Sort: sort,
            Descending: desc,
            Page: pageNumber,
            PageSize: PAGE_SIZE,
          });
          const response = await fetch(`${API_BASE}/api/leaderboards?${query}`, {
            method: "GET",
          });
          if (!response.ok) {
            showError(await extractHttpError(response));
            return;
          }
          data = await response.json();
        }

        setPlayers(data.items);
        setTotalPages(data.totalPages);
        setTotalPlayers(data.totalPlayers);
      } catch {
        showError("Could not connect to server");
      } finally {
        setLoading(false);
      }
    },
    [showError],
  );

  useEffect(() => {
    fetchLeaderboard(criteria, isDescending, page);
  }, [criteria, isDescending, page, fetchLeaderboard]);

  const sortBy = useCallback(
    (newCriteria) => {
      setPage(1); // changing the sort restarts from the first page
      if (newCriteria === criteria) {
        setIsDescending((prev) => !prev);
      } else {
        setCriteria(newCriteria);
        setIsDescending(true);
      }
    },
    [criteria],
  );

  const goToPage = useCallback(
    (next) => {
      setPage((current) => Math.min(Math.max(1, next), Math.max(1, totalPages)));
    },
    [totalPages],
  );

  return {
    players,
    loading,
    criteria,
    isDescending,
    page,
    totalPages,
    totalPlayers,
    sortBy,
    goToPage,
  };
}
