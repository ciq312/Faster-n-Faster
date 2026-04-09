import { useState, useEffect, useCallback } from "react";
import { extractError } from "../../../shared/utils/extractError";
import { useError } from "../../../shared/components/BannerProvider";

export function useFetchLobbies() {
  const { showError } = useError();
  const [lobbies, setLobbies] = useState([]);
  const [loading, setLoading] = useState(false);

  const fetchLobbies = useCallback(async () => {
    setLoading(true);
    setLobbies([]);
    setTimeout(async () => {
      try {
        const response = await fetch("/api/lobbies");
        if (!response.ok) {
          showError(await extractError(response));
          setLoading(false);
          return;
        }
        const data = await response.json();
        setLobbies(data.lobbies);
      } catch (e) {
        showError(e.message);
      } finally {
        setLoading(false);
      }
    }, 500);
  }, []);

  useEffect(() => {
    fetchLobbies();
  }, [fetchLobbies]);

  return { lobbies, loading, refresh: fetchLobbies };
}
